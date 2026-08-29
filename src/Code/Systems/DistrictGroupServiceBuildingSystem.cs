using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace DistrictGroups
{
    // Controls the visibility of the Service Buildings in the District Groups Overlay
    public partial class DistrictGroupServiceBuildingSystem : GameSystemBase
    {
        private const float kSampleIntervalSeconds = 4f;
        private float m_LastSampleTime = float.NegativeInfinity;

        // Which existing vanilla NotificationIconPrefab category
        private static readonly Dictionary<GroupServiceType, string> kTypeIconPrefabNames = new Dictionary<GroupServiceType, string>
        {
            { GroupServiceType.Police, "Police Station" },
            { GroupServiceType.Fire, "Fire Station" },
            { GroupServiceType.Healthcare, "Hospital" },
            { GroupServiceType.Deathcare, "Deathcare Facility" },
            { GroupServiceType.Garbage, "Garbage Facility" },
            { GroupServiceType.EducationElementary, "School" },
            { GroupServiceType.EducationHighSchool, "School" },
            { GroupServiceType.EducationCollege, "School" },
            { GroupServiceType.EducationUniversity, "School" },
            { GroupServiceType.Post, "Post Facility" },
            { GroupServiceType.Parks, "Park" },
            { GroupServiceType.Welfare, "Welfare Office" },
        };

        private DistrictGroupOverlaySystem m_OverlaySystem;
        private GameScreenUISystem m_GameScreenUISystem;
        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_NotificationIconPrefabQuery;

        private Dictionary<GroupServiceType, EntityQuery> m_TypeQueries;
        private Dictionary<GroupServiceType, byte> m_SchoolEducationLevels;

        private readonly Dictionary<GroupServiceType, Entity> m_IconPrefabEntities = new Dictionary<GroupServiceType, Entity>();

        private bool m_ShowServiceBuildings;
        public bool ShowServiceBuildings => m_ShowServiceBuildings;

        private GroupServiceType m_MarkedType = GroupServiceType.Generic;

        private readonly Dictionary<Entity, Entity> m_Markers = new Dictionary<Entity, Entity>();

        // Reused scratch buffers for batching work and avoiding GC pressure with creating these frequently
        private readonly HashSet<Entity> m_TargetBuffer = new HashSet<Entity>();
        private readonly List<Entity> m_StaleBuffer = new List<Entity>();
        private readonly List<Entity> m_PendingCreateBuffer = new List<Entity>();
        private readonly List<Entity> m_PendingDestroyBuffer = new List<Entity>();

        private bool IsActive => m_ShowServiceBuildings && m_OverlaySystem.Visible && !m_GameScreenUISystem.isMenuActive;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlaySystem = World.GetOrCreateSystemManaged<DistrictGroupOverlaySystem>();
            m_GameScreenUISystem = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_NotificationIconPrefabQuery = GetEntityQuery(
                ComponentType.ReadOnly<NotificationIconData>(),
                ComponentType.ReadOnly<PrefabData>());

            EntityQuery schoolQuery = BuildQuery(ComponentType.ReadOnly<Game.Buildings.School>());
            m_TypeQueries = new Dictionary<GroupServiceType, EntityQuery>
            {
                { GroupServiceType.Police, BuildQuery(ComponentType.ReadOnly<Game.Buildings.PoliceStation>(), ComponentType.ReadOnly<Game.Buildings.Prison>()) },
                { GroupServiceType.Fire, BuildQuery(ComponentType.ReadOnly<Game.Buildings.FireStation>()) },
                { GroupServiceType.Healthcare, BuildQuery(ComponentType.ReadOnly<Game.Buildings.Hospital>()) },
                { GroupServiceType.Deathcare, BuildQuery(ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>()) },
                { GroupServiceType.Garbage, BuildQuery(ComponentType.ReadOnly<Game.Buildings.GarbageFacility>()) },
                { GroupServiceType.EducationElementary, schoolQuery },
                { GroupServiceType.EducationHighSchool, schoolQuery },
                { GroupServiceType.EducationCollege, schoolQuery },
                { GroupServiceType.EducationUniversity, schoolQuery },
                { GroupServiceType.Post, BuildQuery(ComponentType.ReadOnly<Game.Buildings.PostFacility>()) },
                { GroupServiceType.Parks, BuildQuery(ComponentType.ReadOnly<Game.Buildings.Park>()) },
                { GroupServiceType.Welfare, BuildQuery(ComponentType.ReadOnly<Game.Buildings.WelfareOffice>()) },
            };

            // Vanilla's SchoolLevel enum: Elementary=1, HighSchool=2, College=3, University=4.
            m_SchoolEducationLevels = new Dictionary<GroupServiceType, byte>
            {
                { GroupServiceType.EducationElementary, 1 },
                { GroupServiceType.EducationHighSchool, 2 },
                { GroupServiceType.EducationCollege, 3 },
                { GroupServiceType.EducationUniversity, 4 },
            };
        }

        protected override void OnDestroy()
        {
            ClearAllMarkers();
            base.OnDestroy();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // clear ours explicitly, so a marker from the previous city doesn't survive as a dangling ghost.
            ClearAllMarkers();
            m_MarkedType = GroupServiceType.Generic;
            m_LastSampleTime = float.NegativeInfinity;
        }

        protected override void OnUpdate()
        {
            bool shouldSample = UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;
            if (shouldSample)
            {
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
            }

            GroupServiceType desiredType = IsActive ? (GroupServiceType)m_OverlaySystem.TypeFilter : GroupServiceType.Generic;
            bool typeChanged = desiredType != m_MarkedType;

            // Even with no type change, re-sample periodically while a real category is showing so
            // newly-constructed or demolished matching buildings get picked up/dropped.
            if (typeChanged || (desiredType != GroupServiceType.Generic && shouldSample))
            {
                RebuildMarkers(desiredType);
            }
        }

        // The panel's own "Show service buildings" checkbox.
        public void SetShowServiceBuildings(bool show)
        {
            if (m_ShowServiceBuildings == show)
            {
                return;
            }
            m_ShowServiceBuildings = show;
            Mod.log.Info($"Show service buildings toggled; show:{m_ShowServiceBuildings}");
        }

        // Wipes every marker this system has ever added to the world.
        public void RemoveAllData()
        {
            Mod.log.Info("Removing all service-building marker state from the world");
            m_ShowServiceBuildings = false;
            RebuildMarkers(GroupServiceType.Generic);
            Mod.log.Info("Finished removing all service-building marker state from the world");
        }

        private EntityQuery BuildQuery(params ComponentType[] anyMarkers)
        {
            return GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Game.Buildings.Building>(), ComponentType.ReadOnly<PrefabRef>() },
                Any = anyMarkers,
                None = new[]
                {
                    ComponentType.ReadOnly<Owner>(),
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                },
            });
        }

        private void RebuildMarkers(GroupServiceType type)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            using NativeArray<Entity> targets = GetTargetBuildings(type, Allocator.Temp);
            double queryMs = stopwatch.Elapsed.TotalMilliseconds;

            m_TargetBuffer.Clear();
            foreach (Entity building in targets)
            {
                m_TargetBuffer.Add(building);
            }

            m_StaleBuffer.Clear();
            foreach (Entity building in m_Markers.Keys)
            {
                if (!m_TargetBuffer.Contains(building))
                {
                    m_StaleBuffer.Add(building);
                }
            }
            m_PendingDestroyBuffer.Clear();
            foreach (Entity building in m_StaleBuffer)
            {
                m_PendingDestroyBuffer.Add(m_Markers[building]);
                m_Markers.Remove(building);
            }
            double diffMs = stopwatch.Elapsed.TotalMilliseconds - queryMs;

            DestroyMarkers(m_PendingDestroyBuffer);
            int removed = m_StaleBuffer.Count;
            double destroyMs = stopwatch.Elapsed.TotalMilliseconds - queryMs - diffMs;

            int added = 0;
            double createMs = 0;
            if (type != GroupServiceType.Generic)
            {
                Entity iconPrefabEntity = ResolveIconPrefabEntity(type);
                if (iconPrefabEntity != Entity.Null)
                {
                    m_PendingCreateBuffer.Clear();
                    foreach (Entity building in m_TargetBuffer)
                    {
                        if (!m_Markers.ContainsKey(building))
                        {
                            m_PendingCreateBuffer.Add(building);
                        }
                    }
                    added = m_PendingCreateBuffer.Count;
                    double beforeCreate = stopwatch.Elapsed.TotalMilliseconds;
                    CreateMarkers(m_PendingCreateBuffer, iconPrefabEntity);
                    createMs = stopwatch.Elapsed.TotalMilliseconds - beforeCreate;
                }
            }

            m_MarkedType = type;

            stopwatch.Stop();
            Mod.log.Info($"Service building markers rebuilt; type:{type} added_count:{added} removed_count:{removed} " +
                $"total_count:{m_Markers.Count} query_ms:{queryMs:F3} diff_ms:{diffMs:F3} destroy_ms:{destroyMs:F3} " +
                $"create_ms:{createMs:F3} duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        private NativeArray<Entity> GetTargetBuildings(GroupServiceType type, Allocator allocator)
        {
            if (!m_TypeQueries.TryGetValue(type, out EntityQuery query))
            {
                return new NativeArray<Entity>(0, allocator);
            }

            if (!m_SchoolEducationLevels.TryGetValue(type, out byte requiredLevel))
            {
                return query.ToEntityArray(allocator);
            }

            using NativeArray<Entity> candidates = query.ToEntityArray(Allocator.Temp);
            using NativeList<Entity> filtered = new NativeList<Entity>(candidates.Length, Allocator.Temp);
            foreach (Entity building in candidates)
            {
                Entity prefab = EntityManager.GetComponentData<PrefabRef>(building).m_Prefab;
                if (EntityManager.HasComponent<SchoolData>(prefab)
                    && EntityManager.GetComponentData<SchoolData>(prefab).m_EducationLevel == requiredLevel)
                {
                    filtered.Add(building);
                }
            }
            return filtered.ToArray(allocator);
        }

        // Finds the entity of the existing NotificationIconPrefab named for this type
        private Entity ResolveIconPrefabEntity(GroupServiceType type)
        {
            if (m_IconPrefabEntities.TryGetValue(type, out Entity cached))
            {
                return cached;
            }
            if (!kTypeIconPrefabNames.TryGetValue(type, out string prefabName))
            {
                return Entity.Null;
            }

            Entity resolved = Entity.Null;
            using NativeArray<Entity> entities = m_NotificationIconPrefabQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity entity in entities)
            {
                PrefabData prefabData = EntityManager.GetComponentData<PrefabData>(entity);
                NotificationIconPrefab prefab = m_PrefabSystem.GetPrefab<NotificationIconPrefab>(prefabData);
                if (prefab.name == prefabName)
                {
                    resolved = entity;
                    break;
                }
            }

            if (resolved == Entity.Null)
            {
                Mod.log.Warn($"Could not resolve notification icon prefab; type:{type} prefab_name:{prefabName}");
            }
            m_IconPrefabEntities[type] = resolved;
            return resolved;
        }

        // Creates all markers for `buildings` as a single batch
        private void CreateMarkers(List<Entity> buildings, Entity iconPrefabEntity)
        {
            if (buildings.Count == 0)
            {
                return;
            }

            NotificationIconData notificationIconData = EntityManager.GetComponentData<NotificationIconData>(iconPrefabEntity);

            using NativeArray<Entity> markers = new NativeArray<Entity>(buildings.Count, Allocator.Temp);
            EntityManager.CreateEntity(notificationIconData.m_Archetype, markers);
            EntityManager.AddComponent<Game.Common.Target>(markers);
            EntityManager.AddComponent<Game.Notifications.DisallowCluster>(markers);

            PrefabRef prefabRef = new PrefabRef(iconPrefabEntity);
            for (int i = 0; i < markers.Length; i++)
            {
                Entity marker = markers[i];
                Entity building = buildings[i];
                EntityManager.SetComponentData(marker, prefabRef);
                EntityManager.SetComponentData(marker, new Game.Notifications.Icon
                {
                    m_Priority = Game.Notifications.IconPriority.Info,
                    m_Flags = Game.Notifications.IconFlags.Unique | Game.Notifications.IconFlags.OnTop,
                    m_Location = GetMarkerLocation(building),
                });
                EntityManager.SetComponentData(marker, new Game.Common.Target(building));
                m_Markers[building] = marker;
            }
        }

        // Roof height (building's own Y plus its prefab's local bounds top), not ground level
        private float3 GetMarkerLocation(Entity building)
        {
            Game.Objects.Transform transform = EntityManager.GetComponentData<Game.Objects.Transform>(building);
            float topY = transform.m_Position.y;
            if (EntityManager.HasComponent<PrefabRef>(building))
            {
                Entity prefab = EntityManager.GetComponentData<PrefabRef>(building).m_Prefab;
                if (EntityManager.HasComponent<ObjectGeometryData>(prefab))
                {
                    topY += EntityManager.GetComponentData<ObjectGeometryData>(prefab).m_Bounds.max.y;
                }
            }
            return new float3(transform.m_Position.x, topY, transform.m_Position.z);
        }

        // Marks every marker in `markers` Deleted as a single batch
        private void DestroyMarkers(List<Entity> markers)
        {
            if (markers.Count == 0)
            {
                return;
            }

            using NativeList<Entity> existing = new NativeList<Entity>(markers.Count, Allocator.Temp);
            foreach (Entity marker in markers)
            {
                if (EntityManager.Exists(marker))
                {
                    existing.Add(marker);
                }
            }
            if (existing.Length > 0)
            {
                EntityManager.AddComponent<Deleted>(existing.AsArray());
            }
        }

        private void ClearAllMarkers()
        {
            m_PendingDestroyBuffer.Clear();
            m_PendingDestroyBuffer.AddRange(m_Markers.Values);
            DestroyMarkers(m_PendingDestroyBuffer);
            m_Markers.Clear();
        }
    }
}

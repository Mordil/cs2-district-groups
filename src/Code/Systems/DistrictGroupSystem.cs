using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DistrictGroups
{
    // System that actually owns district-group entities and expands group membership into ServiceDistrict buffers of assigned buildings.
    //
    // Persistence: groups are ordinary entities whose components implement ISerializable, so the game's save system handles them.
    public partial class DistrictGroupSystem : GameSystemBase
    {
        // Default colors that are assigned in order to groups as they are created.
        private static readonly Color[] kPalette =
        {
            new Color(0.90f, 0.30f, 0.25f, 1f), // red
            new Color(0.25f, 0.55f, 0.95f, 1f), // blue
            new Color(0.30f, 0.80f, 0.40f, 1f), // green
            new Color(0.95f, 0.75f, 0.20f, 1f), // amber
            new Color(0.70f, 0.40f, 0.90f, 1f), // purple
            new Color(0.20f, 0.80f, 0.80f, 1f), // teal
            new Color(0.95f, 0.50f, 0.75f, 1f), // pink
            new Color(0.60f, 0.75f, 0.20f, 1f), // olive
            new Color(0.95f, 0.55f, 0.15f, 1f), // orange
            new Color(0.45f, 0.50f, 0.95f, 1f), // indigo
            new Color(0.55f, 0.35f, 0.20f, 1f), // brown
            new Color(0.55f, 0.60f, 0.65f, 1f), // slate
        };

        private EntityQuery m_GroupQuery;
        private EntityQuery m_AssignmentQuery;

        // Bumped on every group/assignment mutation (including renames and per-building assignment)
        public int Version { get; private set; }
        // Bumped only when a group's membership, type, or color changes
        public int GroupCompositionVersion { get; private set; }

        // Next palette index to hand out to a newly created group.
        private int m_NextColorIndex;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_AssignmentQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupAssignment>());
            InitializeDebugSupport();
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        // The game only purges entities it knows about when a city unloads, so mod-created group entities survive into the next session as ghosts with dangling references
        // Purge before every load; the incoming save then deserializes its own groups fresh.
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            int count = m_GroupQuery.CalculateEntityCount();
            if (count > 0)
            {
                Mod.log.Info($"Purging leftover groups on preload; purpose:{purpose} mode:{mode} count:{count}");
                EntityManager.DestroyEntity(m_GroupQuery);
            }
            m_NextColorIndex = 0;
        }

        // Safety net for saves that already contain corrupted groups: drop member entries whose district no longer exists.
        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode != GameMode.Game)
            {
                return;
            }

            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity group in groups)
            {
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
                int pruned = 0;
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    Entity district = members[i].m_District;
                    if (!EntityManager.Exists(district) || !EntityManager.HasComponent<District>(district))
                    {
                        members.RemoveAt(i);
                        pruned++;
                    }
                }
                if (pruned > 0)
                {
                    Mod.log.Info($"Load cleanup pruned dangling members; group:{GetGroupName(group)} count:{pruned}");
                    ReexpandGroup(group);
                }
            }

            // Saves from before m_Color existed deserialize it with a negative alpha; hand those
            // groups a real palette color now so every group ends the load with an intrinsic color.
            foreach (Entity group in groups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
                if (data.m_Color.a < 0f)
                {
                    data.m_Color = kPalette[m_NextColorIndex++ % kPalette.Length];
                    EntityManager.SetComponentData(group, data);
                    Mod.log.Info($"Assigned color to legacy group; group:{GetGroupName(group)}");
                }
            }

            m_NextColorIndex = groups.Length;
        }

        public Entity CreateGroup(string name, GroupServiceType type)
        {
            Mod.log.Info($"Creating new group; type:{type}");
            Entity group = EntityManager.CreateEntity();
            Color color = kPalette[m_NextColorIndex++ % kPalette.Length];
            EntityManager.AddComponentData(group, new DistrictGroupData { m_Name = name, m_Type = type, m_Color = color });
            EntityManager.AddBuffer<DistrictGroupMember>(group);
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Finished creating new group; type:{type}");
            return group;
        }

        public void DeleteGroup(Entity group)
        {
            Mod.log.Info($"Deleting group; group:{group}");
            using NativeArray<Entity> buildings = GetAssignedBuildings(group, Allocator.Temp);
            foreach (Entity building in buildings)
            {
                UnassignBuilding(building);
            }
            EntityManager.DestroyEntity(group);
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Finished deleting group; group:{group}");
        }

        public void RenameGroup(Entity group, string name)
        {
            Mod.log.Info($"Renaming group; group:{group} name:{name}");
            DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
            data.m_Name = name;
            EntityManager.SetComponentData(group, data);
            Version++;
            Mod.log.Info($"Finished renaming group; group:{group} name:{name}");
        }

        public void SetGroupType(Entity group, GroupServiceType type)
        {
            Mod.log.Info($"Setting group type; group:{group} type:{type}");
            DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
            data.m_Type = type;
            EntityManager.SetComponentData(group, data);
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Finished setting group type; group:{group} type:{type}");
        }

        public void SetGroupColor(Entity group, Color color)
        {
            Mod.log.Info($"Setting group color; group:{group}");
            DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
            data.m_Color = color;
            EntityManager.SetComponentData(group, data);
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Finished setting group color; group:{group}");
        }

        public bool AddMember(Entity group, Entity district)
        {
            Mod.log.Info($"Adding district to group; district:{district} group:{group}");
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
            foreach (DistrictGroupMember member in members)
            {
                if (member.m_District == district)
                {
                    Mod.log.Info($"District already in group, skipping; district:{district} group:{group}");
                    return false;
                }
            }
            members.Add(new DistrictGroupMember(district));
            ReexpandGroup(group);
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Finished adding district to group; district:{district} group:{group}");
            return true;
        }

        public bool RemoveMember(Entity group, Entity district)
        {
            Mod.log.Info($"Removing district from group; district:{district} group:{group}");
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].m_District == district)
                {
                    members.RemoveAt(i);
                    ReexpandGroup(group);
                    Version++;
                    GroupCompositionVersion++;
                    Mod.log.Info($"Finished removing district from group; district:{district} group:{group}");
                    return true;
                }
            }
            Mod.log.Info($"District not found in group, skipping; district:{district} group:{group}");
            return false;
        }

        // Updates the member districts of the provided group
        public void SetMembers(Entity group, DynamicBuffer<ServiceDistrict> desiredDistricts)
        {
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
            members.Clear();
            foreach (ServiceDistrict entry in desiredDistricts)
            {
                members.Add(new DistrictGroupMember(entry.m_District));
            }
            Version++;
            GroupCompositionVersion++;
            Mod.log.Info($"Set group members from selection; group:{group} count:{members.Length}");
        }

        // while assigned, the group owns the building's entire ServiceDistrict buffer content.
        public bool AssignBuilding(Entity building, Entity group)
        {
            Mod.log.Info($"Assigning group to building; group:{group} building:{building}");
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (!EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                Mod.log.Error($"Cannot assign group, building has no ServiceDistrict buffer; building:{building}");
                return false;
            }
            if (EntityManager.HasComponent<DistrictGroupAssignment>(building))
            {
                EntityManager.SetComponentData(building, new DistrictGroupAssignment(group));
                EntityManager.SetComponentEnabled<DistrictGroupAssignment>(building, true);
            }
            else
            {
                EntityManager.AddComponentData(building, new DistrictGroupAssignment(group));
            }
            double assignmentMs = stopwatch.Elapsed.TotalMilliseconds;
            ExpandToBuilding(building, group);
            Version++;
            double totalMs = stopwatch.Elapsed.TotalMilliseconds;
            Mod.log.Debug($"Finished assigning group to building; group:{group} building:{building} " +
                $"duration_ms:{totalMs:F3} assignment_ms:{assignmentMs:F3} expand_ms:{totalMs - assignmentMs:F3}");
            return true;
        }

        public bool UnassignBuilding(Entity building)
        {
            Mod.log.Info($"Unassigning building; building:{building}");
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            if (!EntityManager.HasComponent<DistrictGroupAssignment>(building)
                || EntityManager.GetComponentData<DistrictGroupAssignment>(building).m_Group == Entity.Null)
            {
                Mod.log.Info($"Building has no group assignment, skipping; building:{building}");
                return false;
            }
            // m_Group is the source of truth for "assigned" (robust even if the enabled bit doesn't
            // round-trip through save/load); disabling too is what keeps this off the query fast path.
            EntityManager.SetComponentData(building, new DistrictGroupAssignment(Entity.Null));
            EntityManager.SetComponentEnabled<DistrictGroupAssignment>(building, false);
            double assignmentMs = stopwatch.Elapsed.TotalMilliseconds;
            if (EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                EntityManager.GetBuffer<ServiceDistrict>(building).Clear();
            }
            Version++;
            double totalMs = stopwatch.Elapsed.TotalMilliseconds;
            Mod.log.Debug($"Finished unassigning building; building:{building} " +
                $"duration_ms:{totalMs:F3} assignment_ms:{assignmentMs:F3} expand_ms:{totalMs - assignmentMs:F3}");
            return true;
        }

        public void ReexpandGroup(Entity group)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            using NativeArray<Entity> buildings = GetAssignedBuildings(group, Allocator.Temp);
            double lookupMs = stopwatch.Elapsed.TotalMilliseconds;
            using NativeArray<Entity> validDistricts = GetValidMemberDistricts(group, Allocator.Temp);
            foreach (Entity building in buildings)
            {
                ExpandToBuilding(building, validDistricts);
            }
            Mod.log.Debug($"Reexpanded group; group:{GetGroupName(group)} duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} " +
                $"lookup_ms:{lookupMs:F3} building_count:{buildings.Length} district_count:{validDistricts.Length}");
        }

        private void ExpandToBuilding(Entity building, Entity group)
        {
            using NativeArray<Entity> validDistricts = GetValidMemberDistricts(group, Allocator.Temp);
            ExpandToBuilding(building, validDistricts);
        }

        private void ExpandToBuilding(Entity building, NativeArray<Entity> validDistricts)
        {
            DynamicBuffer<ServiceDistrict> serviceDistricts = EntityManager.GetBuffer<ServiceDistrict>(building);
            serviceDistricts.Clear();
            foreach (Entity district in validDistricts)
            {
                serviceDistricts.Add(new ServiceDistrict(district));
            }
        }

        // membership pruning happens in DistrictGroupSyncSystem and on load,
        // but a dead district must never reach a vanilla buffer.
        // Filtered once per group instead of once per assigned building.
        private NativeArray<Entity> GetValidMemberDistricts(Entity group, Allocator allocator)
        {
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
            using NativeList<Entity> valid = new NativeList<Entity>(members.Length, Allocator.Temp);
            foreach (DistrictGroupMember member in members)
            {
                Entity district = member.m_District;
                if (!EntityManager.Exists(district)
                    || !EntityManager.HasComponent<District>(district)
                    || EntityManager.HasComponent<Game.Common.Deleted>(district))
                {
                    continue;
                }
                valid.Add(district);
            }
            return valid.ToArray(allocator);
        }

        public NativeArray<Entity> GetGroups(Allocator allocator)
        {
            return m_GroupQuery.ToEntityArray(allocator);
        }

        public bool HasGroups => !m_GroupQuery.IsEmptyIgnoreFilter;

        public NativeArray<Entity> GetAssignedBuildings(Entity group, Allocator allocator)
        {
            using NativeArray<Entity> candidates = m_AssignmentQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<DistrictGroupAssignment> assignments = m_AssignmentQuery.ToComponentDataArray<DistrictGroupAssignment>(Allocator.Temp);
            using NativeList<Entity> result = new NativeList<Entity>(candidates.Length, Allocator.Temp);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (assignments[i].m_Group == group)
                {
                    result.Add(candidates[i]);
                }
            }
            return result.ToArray(allocator);
        }

        public Entity FindGroupByName(string name)
        {
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity group in groups)
            {
                if (EntityManager.GetComponentData<DistrictGroupData>(group).m_Name.ToString() == name)
                {
                    return group;
                }
            }
            return Entity.Null;
        }

        public string GetGroupName(Entity group)
        {
            return EntityManager.Exists(group) && EntityManager.HasComponent<DistrictGroupData>(group)
                ? EntityManager.GetComponentData<DistrictGroupData>(group).m_Name.ToString()
                : "<missing>";
        }
    }
}

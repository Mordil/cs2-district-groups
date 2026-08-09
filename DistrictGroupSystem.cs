using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;

namespace multi_district_tool
{
    // Phase 2 registry (see CLAUDE_IMPL_PLAN.md): owns district-group entities and
    // expands group membership into the vanilla ServiceDistrict buffers of assigned
    // buildings. API-only for now — no per-frame work until Phase 3 (sync).
    //
    // Persistence: groups are ordinary entities whose components implement
    // ISerializable, so the game's save system stores them and remaps the Entity
    // references on load (verified in Phase 1 for vanilla buffers).
    public partial class DistrictGroupSystem : GameSystemBase
    {
        private EntityQuery m_GroupQuery;
        private EntityQuery m_AssignmentQuery;

        // Bumped on every group/assignment mutation so UI systems can refresh.
        public int Version { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_AssignmentQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupAssignment>());
            Enabled = false;
        }

        protected override void OnUpdate()
        {
        }

        // The game only purges entities it knows about when a city unloads, so
        // mod-created group entities survive into the next session as ghosts with
        // dangling references (observed in Phase 2 testing). Purge before every
        // load; the incoming save then deserializes its own groups fresh.
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            int count = m_GroupQuery.CalculateEntityCount();
            if (count > 0)
            {
                Mod.log.Info($"OnGamePreload({purpose}, {mode}): purging {count} leftover group entity(ies).");
                EntityManager.DestroyEntity(m_GroupQuery);
            }
        }

        // Safety net for saves that already contain corrupted groups: drop member
        // entries whose district no longer exists, then delete groups left with no
        // members and no assigned buildings.
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
                    Mod.log.Info($"Load cleanup: pruned {pruned} dangling member(s) from group \"{GetGroupName(group)}\".");
                    ReexpandGroup(group);
                }

                using NativeArray<Entity> assigned = GetAssignedBuildings(group, Allocator.Temp);
                if (members.Length == 0 && assigned.Length == 0)
                {
                    Mod.log.Info($"Load cleanup: deleting empty, unassigned group \"{GetGroupName(group)}\" {group}.");
                    EntityManager.DestroyEntity(group);
                }
            }
        }

        public Entity CreateGroup(string name, GroupServiceType type)
        {
            Entity group = EntityManager.CreateEntity();
            EntityManager.AddComponentData(group, new DistrictGroupData { m_Name = name, m_Type = type });
            EntityManager.AddBuffer<DistrictGroupMember>(group);
            Version++;
            Mod.log.Info($"Created group {group} \"{name}\" ({type})");
            return group;
        }

        public void DeleteGroup(Entity group)
        {
            using NativeArray<Entity> buildings = GetAssignedBuildings(group, Allocator.Temp);
            foreach (Entity building in buildings)
            {
                UnassignBuilding(building);
            }
            Mod.log.Info($"Deleting group {group} \"{GetGroupName(group)}\" ({buildings.Length} building(s) unassigned)");
            EntityManager.DestroyEntity(group);
            Version++;
        }

        public void RenameGroup(Entity group, string name)
        {
            DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
            data.m_Name = name;
            EntityManager.SetComponentData(group, data);
            Version++;
        }

        public void SetGroupType(Entity group, GroupServiceType type)
        {
            DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
            data.m_Type = type;
            EntityManager.SetComponentData(group, data);
            Version++;
        }

        public bool AddMember(Entity group, Entity district)
        {
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
            foreach (DistrictGroupMember member in members)
            {
                if (member.m_District == district)
                {
                    return false;
                }
            }
            members.Add(new DistrictGroupMember(district));
            ReexpandGroup(group);
            Version++;
            return true;
        }

        public bool RemoveMember(Entity group, Entity district)
        {
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
            for (int i = 0; i < members.Length; i++)
            {
                if (members[i].m_District == district)
                {
                    members.RemoveAt(i);
                    ReexpandGroup(group);
                    Version++;
                    return true;
                }
            }
            return false;
        }

        // v1 is exclusive: while assigned, the group owns the building's entire
        // ServiceDistrict buffer content.
        public bool AssignBuilding(Entity building, Entity group)
        {
            if (!EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                Mod.log.Info($"Cannot assign group: {building} has no ServiceDistrict buffer.");
                return false;
            }
            if (EntityManager.HasComponent<DistrictGroupAssignment>(building))
            {
                EntityManager.SetComponentData(building, new DistrictGroupAssignment(group));
            }
            else
            {
                EntityManager.AddComponentData(building, new DistrictGroupAssignment(group));
            }
            ExpandToBuilding(building, group);
            Version++;
            Mod.log.Info($"Assigned group \"{GetGroupName(group)}\" to {building}.");
            return true;
        }

        public bool UnassignBuilding(Entity building)
        {
            if (!EntityManager.HasComponent<DistrictGroupAssignment>(building))
            {
                return false;
            }
            EntityManager.RemoveComponent<DistrictGroupAssignment>(building);
            if (EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                EntityManager.GetBuffer<ServiceDistrict>(building).Clear();
            }
            Version++;
            Mod.log.Info($"Unassigned group from {building}; it serves the whole city again.");
            return true;
        }

        public void ReexpandGroup(Entity group)
        {
            using NativeArray<Entity> buildings = GetAssignedBuildings(group, Allocator.Temp);
            foreach (Entity building in buildings)
            {
                ExpandToBuilding(building, group);
            }
        }

        private void ExpandToBuilding(Entity building, Entity group)
        {
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
            DynamicBuffer<ServiceDistrict> serviceDistricts = EntityManager.GetBuffer<ServiceDistrict>(building);
            serviceDistricts.Clear();
            foreach (DistrictGroupMember member in members)
            {
                // Backstop: membership pruning happens in DistrictGroupSyncSystem
                // and on load, but a dead district must never reach a vanilla buffer.
                Entity district = member.m_District;
                if (!EntityManager.Exists(district)
                    || !EntityManager.HasComponent<District>(district)
                    || EntityManager.HasComponent<Game.Common.Deleted>(district))
                {
                    continue;
                }
                serviceDistricts.Add(new ServiceDistrict(district));
            }
        }

        public NativeArray<Entity> GetGroups(Allocator allocator)
        {
            return m_GroupQuery.ToEntityArray(allocator);
        }

        public NativeArray<Entity> GetAssignedBuildings(Entity group, Allocator allocator)
        {
            using NativeArray<Entity> candidates = m_AssignmentQuery.ToEntityArray(Allocator.Temp);
            using NativeList<Entity> result = new NativeList<Entity>(candidates.Length, Allocator.Temp);
            foreach (Entity building in candidates)
            {
                if (EntityManager.GetComponentData<DistrictGroupAssignment>(building).m_Group == group)
                {
                    result.Add(building);
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

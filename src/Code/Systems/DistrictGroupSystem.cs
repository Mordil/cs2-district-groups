using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    // System that actually owns district-group entities and expands group membership into ServiceDistrict buffers of assigned buildings.
    //
    // Persistence: groups are ordinary entities whose components implement ISerializable, so the game's save system handles them.
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
        }

        public Entity CreateGroup(string name, GroupServiceType type)
        {
            Mod.log.Info($"Creating new group; type:{type}");
            Entity group = EntityManager.CreateEntity();
            EntityManager.AddComponentData(group, new DistrictGroupData { m_Name = name, m_Type = type });
            EntityManager.AddBuffer<DistrictGroupMember>(group);
            Version++;
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
            Mod.log.Info($"Finished setting group type; group:{group} type:{type}");
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
                    Mod.log.Info($"Finished removing district from group; district:{district} group:{group}");
                    return true;
                }
            }
            Mod.log.Info($"District not found in group, skipping; district:{district} group:{group}");
            return false;
        }

        // while assigned, the group owns the building's entire ServiceDistrict buffer content.
        public bool AssignBuilding(Entity building, Entity group)
        {
            Mod.log.Info($"Assigning group to building; group:{group} building:{building}");
            if (!EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                Mod.log.Info($"Cannot assign group, building has no ServiceDistrict buffer; building:{building}");
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
            Mod.log.Info($"Finished assigning group to building; group:{group} building:{building}");
            return true;
        }

        public bool UnassignBuilding(Entity building)
        {
            Mod.log.Info($"Unassigning building; building:{building}");
            if (!EntityManager.HasComponent<DistrictGroupAssignment>(building))
            {
                Mod.log.Info($"Building has no group assignment, skipping; building:{building}");
                return false;
            }
            EntityManager.RemoveComponent<DistrictGroupAssignment>(building);
            if (EntityManager.HasBuffer<ServiceDistrict>(building))
            {
                EntityManager.GetBuffer<ServiceDistrict>(building).Clear();
            }
            Version++;
            Mod.log.Info($"Finished unassigning building; building:{building}");
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

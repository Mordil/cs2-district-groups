using Game;
using Game.Areas;
using Game.Common;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace multi_district_tool
{
    // Phase 3: live sync. When a district is deleted during play, prune it from
    // all groups and re-expand affected buildings. (Vanilla's ServiceDistrictSystem
    // prunes the buildings' ServiceDistrict buffers itself; this keeps the group
    // definitions from dangling and re-adding dead refs on a later expansion.)
    public partial class DistrictGroupSyncSystem : GameSystemBase
    {
        private EntityQuery m_DeletedDistrictQuery;
        private EntityQuery m_GroupQuery;
        private DistrictGroupSystem m_GroupSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DeletedDistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadOnly<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            RequireForUpdate(m_DeletedDistrictQuery);
        }

        protected override void OnUpdate()
        {
            using NativeArray<Entity> deleted = m_DeletedDistrictQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity group in groups)
            {
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group);
                int removed = 0;
                for (int i = members.Length - 1; i >= 0; i--)
                {
                    foreach (Entity deletedDistrict in deleted)
                    {
                        if (members[i].m_District == deletedDistrict)
                        {
                            members.RemoveAt(i);
                            removed++;
                            break;
                        }
                    }
                }
                if (removed > 0)
                {
                    Mod.log.Info($"Sync: removed {removed} deleted district(s) from group \"{m_GroupSystem.GetGroupName(group)}\"; re-expanding.");
                    m_GroupSystem.ReexpandGroup(group);
                }
            }
        }
    }
}

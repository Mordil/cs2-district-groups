using Game;
using Game.Areas;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    // "District selection mode": lets the user click districts in the world to
    // add/remove them from a group's member list, mirroring vanilla's own
    // "Select operating districts" tool on service buildings
    //
    // The vanilla tool only knows how to write into a ServiceDistrict buffer on
    // whatever entity is set as its selectionOwner. Rather than attach that
    // buffer to our persisted group entities,
    // give the tool one disposable scratch entity, and mirror its
    // buffer contents into the real group's DistrictGroupMember list each frame
    // while active.
    public partial class DistrictGroupSelectionSystem : GameSystemBase
    {
        private ToolSystem m_ToolSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private SelectionToolSystem m_SelectionToolSystem;
        private DistrictGroupSystem m_GroupSystem;

        private Entity m_ScratchEntity;
        private Entity m_SelectingGroup = Entity.Null;
        public Entity SelectingGroup => m_SelectingGroup;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_SelectionToolSystem = World.GetOrCreateSystemManaged<SelectionToolSystem>();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();

            m_ScratchEntity = EntityManager.CreateEntity();
            EntityManager.AddBuffer<ServiceDistrict>(m_ScratchEntity);
            EntityManager.SetName(m_ScratchEntity, "DistrictGroups scratch selection owner");

            // if the player switches to a different tool, just toggle off
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Combine(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
        }

        protected override void OnDestroy()
        {
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Remove(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
            base.OnDestroy();
        }

        private void OnActiveToolChanged(ToolBaseSystem tool)
        {
            bool stillOurs = tool == m_SelectionToolSystem && m_SelectionToolSystem.selectionOwner == m_ScratchEntity;
            if (m_SelectingGroup != Entity.Null && !stillOurs)
            {
                SyncScratchIntoGroup(m_SelectingGroup);
                m_SelectingGroup = Entity.Null;
            }
        }

        public void ToggleSelection(Entity group)
        {
            if (m_SelectingGroup == group)
            {
                Mod.log.Info($"Stopping district selection; group:{group}");
                SyncScratchIntoGroup(group);
                m_SelectingGroup = Entity.Null;
                m_ToolSystem.activeTool = m_DefaultToolSystem;
                Mod.log.Info($"Finished stopping district selection; group:{group}");
                return;
            }

            if (m_SelectingGroup != Entity.Null)
            {
                SyncScratchIntoGroup(m_SelectingGroup);
            }

            Mod.log.Info($"Starting district selection; group:{group}");
            m_SelectingGroup = group;
            SeedScratchFromGroup(group);
            m_SelectionToolSystem.selectionOwner = m_ScratchEntity;
            m_SelectionToolSystem.selectionType = SelectionType.ServiceDistrict;
            m_ToolSystem.activeTool = m_SelectionToolSystem;
            Mod.log.Info($"Finished starting district selection; group:{group}");
        }

        protected override void OnUpdate()
        {
            if (m_SelectingGroup == Entity.Null || !EntityManager.Exists(m_SelectingGroup))
            {
                return;
            }
            SyncScratchIntoGroup(m_SelectingGroup);
        }

        // Show existing member districts as already highlighted
        private void SeedScratchFromGroup(Entity group)
        {
            DynamicBuffer<ServiceDistrict> scratch = EntityManager.GetBuffer<ServiceDistrict>(m_ScratchEntity);
            scratch.Clear();
            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
            foreach (DistrictGroupMember member in members)
            {
                scratch.Add(new ServiceDistrict(member.m_District));
            }
        }

        // Forward data events one-way into the target group.
        private void SyncScratchIntoGroup(Entity group)
        {
            if (!EntityManager.Exists(group))
            {
                return;
            }

            DynamicBuffer<ServiceDistrict> scratch = EntityManager.GetBuffer<ServiceDistrict>(m_ScratchEntity, isReadOnly: true);
            using NativeList<Entity> scratchDistricts = new NativeList<Entity>(scratch.Length, Allocator.Temp);
            foreach (ServiceDistrict entry in scratch)
            {
                scratchDistricts.Add(entry.m_District);
            }

            DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
            using NativeList<Entity> currentMembers = new NativeList<Entity>(members.Length, Allocator.Temp);
            foreach (DistrictGroupMember member in members)
            {
                currentMembers.Add(member.m_District);
            }

            foreach (Entity district in scratchDistricts)
            {
                if (!currentMembers.Contains(district))
                {
                    m_GroupSystem.AddMember(group, district);
                }
            }
            foreach (Entity district in currentMembers)
            {
                if (!scratchDistricts.Contains(district))
                {
                    m_GroupSystem.RemoveMember(group, district);
                }
            }
        }
    }
}

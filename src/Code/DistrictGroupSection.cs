using Colossal.UI.Binding;
using Game.Areas;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using static DistrictGroups.EntityJson;

namespace DistrictGroups
{
    /* Renaming this class will break the UI unless it's changed to the new name, since we're referencing things by exact type names as strings */

    // A custom section to be injected in the info panel of service buildings
    // Allows interaction with the building's district group assignment
    public partial class DistrictGroupSection : InfoSectionBase
    {
        protected override string group => "DistrictGroupSection";

        private DistrictGroupSystem m_GroupSystem;
        private EntityQuery m_GroupQuery;
        private Entity m_AssignedGroup;
        private GroupServiceType m_BuildingType;
        private int m_LastSeenVersion = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());

            // Sits directly above the "operating districts" section the assignment governs.
            InfoSectionOrder.InsertBefore<DistrictsSection>(m_InfoUISystem, this);

            AddBinding(new TriggerBinding<Entity>(DistrictGroupsUISystem.kBindingGroup, "assignGroup", OnAssignGroup));
            AddBinding(new TriggerBinding(DistrictGroupsUISystem.kBindingGroup, "unassignGroup", OnUnassignGroup));
        }

        private void OnAssignGroup(Entity group)
        {
            if (EntityManager.Exists(selectedEntity))
            {
                m_GroupSystem.AssignBuilding(selectedEntity, group);
                RequestUpdate();
            }
        }

        private void OnUnassignGroup()
        {
            if (EntityManager.Exists(selectedEntity))
            {
                m_GroupSystem.UnassignBuilding(selectedEntity);
                RequestUpdate();
            }
        }

        protected override void Reset()
        {
            m_AssignedGroup = Entity.Null;
            m_BuildingType = GroupServiceType.Generic;
        }

        protected override void OnProcess() { }

        protected override void OnUpdate()
        {
            long startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            try
            {
                base.OnUpdate();

                // Group edits made in the manager panel must refresh this section too.
                int version = m_GroupSystem.Version;
                if (version != m_LastSeenVersion)
                {
                    m_LastSeenVersion = version;
                    RequestUpdate();
                }

                visible = EntityManager.Exists(selectedEntity)
                    && EntityManager.HasBuffer<ServiceDistrict>(selectedEntity);
                if (!visible)
                {
                    return;
                }
                m_AssignedGroup = EntityManager.HasComponent<DistrictGroupAssignment>(selectedEntity)
                    ? EntityManager.GetComponentData<DistrictGroupAssignment>(selectedEntity).m_Group
                    : Entity.Null;
                m_BuildingType = m_GroupSystem.DetectServiceType(selectedPrefab);
            }
            finally
            {
                // Diagnostic only: flags a slow frame so we can tell whether a UI stall is inside
                // our own code (an EntityManager call forced to wait on an in-flight job) or elsewhere.
                double elapsedMs = ElapsedMilliseconds(startTimestamp);
                if (elapsedMs > 5.0)
                {
                    Mod.log.Debug($"DistrictGroupSection.OnUpdate slow; duration_ms:{elapsedMs:F3} building:{selectedEntity}");
                }
            }
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            long startTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            writer.PropertyName("buildingType");
            writer.Write((int)m_BuildingType);
            writer.PropertyName("hasAssignment");
            writer.Write(m_AssignedGroup != Entity.Null);
            writer.PropertyName("assignedGroupName");
            writer.Write(m_AssignedGroup != Entity.Null ? m_GroupSystem.GetGroupName(m_AssignedGroup) : "");

            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            using NativeList<Entity> candidates = new NativeList<Entity>(groups.Length, Allocator.Temp);
            foreach (Entity candidate in groups)
            {
                GroupServiceType type = EntityManager.GetComponentData<DistrictGroupData>(candidate).m_Type;
                bool matches = m_BuildingType == GroupServiceType.Generic
                    || type == m_BuildingType
                    || type == GroupServiceType.Generic;
                if (matches && candidate != m_AssignedGroup)
                {
                    candidates.Add(candidate);
                }
            }

            writer.PropertyName("candidates");
            writer.ArrayBegin(candidates.Length);
            foreach (Entity candidate in candidates)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(candidate);
                writer.TypeBegin("GroupOption");
                writer.PropertyName("entity");
                WriteEntity(writer, candidate);
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.PropertyName("color");
                writer.Write(data.m_Color);
                writer.TypeEnd();
            }
            writer.ArrayEnd();

            double elapsedMs = ElapsedMilliseconds(startTimestamp);
            if (elapsedMs > 5.0)
            {
                Mod.log.Debug($"DistrictGroupSection.OnWriteProperties slow; duration_ms:{elapsedMs:F3} building:{selectedEntity}");
            }
        }

        private static double ElapsedMilliseconds(long startTimestamp)
        {
            return (System.Diagnostics.Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }
    }
}

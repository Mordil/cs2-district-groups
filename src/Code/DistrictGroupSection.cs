using System.Collections.Generic;
using System.Reflection;
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

        /*
            We need to use reflection to find the index of the "operating districts" section since it's all private and done before we load

            That way we can insert at the right index
        */
        private static readonly FieldInfo kMiddleSectionsField =
            typeof(SelectedInfoUISystem).GetField("m_MiddleSections", BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            InsertBeforeDistrictsSection();

            AddBinding(new TriggerBinding<Entity>(DistrictGroupsUISystem.kBindingGroup, "assignGroup", OnAssignGroup));
            AddBinding(new TriggerBinding(DistrictGroupsUISystem.kBindingGroup, "unassignGroup", OnUnassignGroup));
        }

        private void InsertBeforeDistrictsSection()
        {
            DistrictsSection districtsSection = World.GetOrCreateSystemManaged<DistrictsSection>();
            if (kMiddleSectionsField?.GetValue(m_InfoUISystem) is List<ISectionSource> sections)
            {
                int index = sections.IndexOf(districtsSection);
                if (index >= 0)
                {
                    sections.Insert(index, this);
                    return;
                }
            }
            Mod.log.Info("Could not locate DistrictsSection in the info panel. falling back to appending our section;");
            m_InfoUISystem.AddMiddleSection(this);
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

        public override void OnWriteProperties(IJsonWriter writer)
        {
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
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }
    }
}

using Colossal.UI.Binding;
using Game.Areas;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;

namespace multi_district_tool
{
    // Phase 5.3: a section on the vanilla selected-building info panel. Visible
    // for buildings that can serve districts; shows the assigned group and offers
    // type-filtered candidates. The JS side keys off this type's FULL NAME
    // ("multi_district_tool.DistrictGroupSection") — renaming breaks the UI.
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
            m_InfoUISystem.AddMiddleSection(this);

            AddBinding(new TriggerBinding<Entity>(MultiDistrictUISystem.kBindingGroup, "assignGroup", OnAssignGroup));
            AddBinding(new TriggerBinding(MultiDistrictUISystem.kBindingGroup, "unassignGroup", OnUnassignGroup));
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
            m_BuildingType = DetectServiceType(selectedPrefab);
        }

        protected override void OnProcess()
        {
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
                writer.TypeBegin("Unity.Entities.Entity");
                writer.PropertyName("index");
                writer.Write(candidate.Index);
                writer.PropertyName("version");
                writer.Write(candidate.Version);
                writer.TypeEnd();
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private GroupServiceType DetectServiceType(Entity prefab)
        {
            if (!EntityManager.Exists(prefab))
            {
                return GroupServiceType.Generic;
            }
            if (EntityManager.HasComponent<PoliceStationData>(prefab)) return GroupServiceType.Police;
            if (EntityManager.HasComponent<FireStationData>(prefab)) return GroupServiceType.Fire;
            if (EntityManager.HasComponent<HospitalData>(prefab)) return GroupServiceType.Healthcare;
            if (EntityManager.HasComponent<DeathcareFacilityData>(prefab)) return GroupServiceType.Deathcare;
            if (EntityManager.HasComponent<GarbageFacilityData>(prefab)) return GroupServiceType.Garbage;
            if (EntityManager.HasComponent<PostFacilityData>(prefab)) return GroupServiceType.Post;
            if (EntityManager.HasComponent<ParkData>(prefab)) return GroupServiceType.Parks;
            if (EntityManager.HasComponent<WelfareOfficeData>(prefab)) return GroupServiceType.Welfare;
            if (EntityManager.HasComponent<SchoolData>(prefab))
            {
                SchoolData school = EntityManager.GetComponentData<SchoolData>(prefab);
                switch (school.m_EducationLevel)
                {
                    case 1: return GroupServiceType.EducationElementary;
                    case 2: return GroupServiceType.EducationHighSchool;
                    case 3: return GroupServiceType.EducationCollege;
                    default: return GroupServiceType.EducationUniversity;
                }
            }
            return GroupServiceType.Generic;
        }
    }
}

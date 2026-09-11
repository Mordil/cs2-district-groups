using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Areas;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using static DistrictGroups.EntityJson;

namespace DistrictGroups
{
    /* Renaming this class will break the UI unless it's changed to the new name, since we're referencing things by exact type names as strings */

    // A custom section to be injected in the info panel of a district
    // Lists every district group the district is a member of
    public partial class DistrictGroupMembershipSection : InfoSectionBase
    {
        protected override string group => "DistrictGroupMembershipSection";

        private DistrictGroupSystem m_GroupSystem;
        private EntityQuery m_GroupQuery;
        private readonly List<Entity> m_MemberGroups = new List<Entity>();
        private int m_LastSeenVersion = -1;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());

            // Keeps the group list next to the vanilla list of buildings serving the district.
            InfoSectionOrder.InsertBefore<LocalServicesSection>(m_InfoUISystem, this);
        }

        protected override void Reset()
        {
            m_MemberGroups.Clear();
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

            m_MemberGroups.Clear();
            bool isDistrict = EntityManager.HasComponent<District>(selectedEntity)
                && EntityManager.HasComponent<Area>(selectedEntity);
            if (isDistrict)
            {
                CollectGroupsContaining(selectedEntity);
            }

            // A district no group has claimed has nothing to say here, so the section stays away entirely.
            visible = m_MemberGroups.Count > 0;
        }

        public override void OnWriteProperties(IJsonWriter writer)
        {
            writer.PropertyName("groups");
            writer.ArrayBegin(m_MemberGroups.Count);
            foreach (Entity memberGroup in m_MemberGroups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(memberGroup);
                writer.TypeBegin("MemberGroup");
                writer.PropertyName("entity");
                WriteEntity(writer, memberGroup);
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        // Every group whose member list names the district.
        private void CollectGroupsContaining(Entity district)
        {
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity candidate in groups)
            {
                DynamicBuffer<DistrictGroupMember> members =
                    EntityManager.GetBuffer<DistrictGroupMember>(candidate, isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    if (member.m_District == district)
                    {
                        m_MemberGroups.Add(candidate);
                        break;
                    }
                }
            }
        }
    }
}

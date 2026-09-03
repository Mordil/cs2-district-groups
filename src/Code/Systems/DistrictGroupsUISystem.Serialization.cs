using Colossal.UI.Binding;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using static DistrictGroups.EntityJson;

namespace DistrictGroups
{
    public partial class DistrictGroupsUISystem
    {
        private void WriteGroups(IJsonWriter writer)
        {
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            Dictionary<Entity, int> districtPopulations = m_GroupSystem.GetDistrictPopulations();
            writer.ArrayBegin(groups.Length);
            foreach (Entity group in groups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
                writer.TypeBegin("Group");
                writer.PropertyName("entity");
                WriteEntity(writer, group);
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.PropertyName("color");
                writer.Write(data.m_Color);
                writer.PropertyName("assignedBuildingCount");
                using (NativeArray<Entity> assignedBuildings = m_GroupSystem.GetAssignedBuildings(group, Allocator.Temp))
                {
                    writer.Write(assignedBuildings.Length);
                }
                writer.PropertyName("population");
                writer.Write(m_GroupSystem.GetPopulation(group, districtPopulations));
                writer.PropertyName("members");
                writer.ArrayBegin(members.Length);
                foreach (DistrictGroupMember member in members)
                {
                    WriteNamedEntity(writer, member.m_District);
                }
                writer.ArrayEnd();
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void WriteServiceBuildings(IJsonWriter writer)
        {
            GroupServiceType type = (GroupServiceType)m_OverlaySystem.TypeFilter;
            using NativeArray<Entity> buildings = m_ServiceBuildingSystem.GetTargetBuildings(type, Allocator.Temp);
            writer.ArrayBegin(buildings.Length);
            foreach (Entity building in buildings)
            {
                WriteServiceBuilding(writer, type, building);
            }
            writer.ArrayEnd();
        }

        // Listed buildings all come from the filtered type's own query,
        // so that type IS every listed building's type
        private void WriteServiceBuilding(IJsonWriter writer, GroupServiceType type, Entity building)
        {
            Entity assignedGroup = EntityManager.HasComponent<DistrictGroupAssignment>(building)
                ? EntityManager.GetComponentData<DistrictGroupAssignment>(building).m_Group
                : Entity.Null;

            writer.TypeBegin("ServiceBuilding");
            writer.PropertyName("entity");
            WriteEntity(writer, building);
            writer.PropertyName("name");
            writer.Write(m_NameSystem.GetRenderedLabelName(building));
            writer.PropertyName("type");
            writer.Write((int)type);
            writer.PropertyName("hasAssignment");
            writer.Write(assignedGroup != Entity.Null);
            writer.PropertyName("assignedGroup");
            WriteEntity(writer, assignedGroup);
            writer.PropertyName("assignedGroupName");
            writer.Write(assignedGroup != Entity.Null ? m_GroupSystem.GetGroupName(assignedGroup) : "");
            writer.TypeEnd();
        }

        private void WriteNamedEntity(IJsonWriter writer, Entity entity)
        {
            writer.TypeBegin("NamedEntity");
            writer.PropertyName("entity");
            WriteEntity(writer, entity);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(entity) ? m_NameSystem.GetRenderedLabelName(entity) : "<missing>");
            writer.TypeEnd();
        }
    }
}

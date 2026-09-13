using System.Text;
using Game.Areas;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    // Debug/troubleshooting tooling
    public partial class DistrictGroupSystem
    {
        private EntityQuery m_ServiceBuildingQuery;
        private EntityQuery m_DistrictQuery;

        private void InitializeDebugSupport()
        {
            m_ServiceBuildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<ServiceDistrict>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.Exclude<Game.Common.Deleted>(),
                ComponentType.Exclude<Game.Tools.Temp>());
        }

        // writes a human-readable, multi-line report to the log file for troubleshooting reports
        public void DumpDebugData()
        {
            int serviceBuildingCount = m_ServiceBuildingQuery.CalculateEntityCount();
            int assignedServiceBuildingCount = m_AssignmentQuery.CalculateEntityCount();
            int districtCount = m_DistrictQuery.CalculateEntityCount();

            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);

            StringBuilder report = new StringBuilder();
            report.AppendLine("=== DATA DUMP ===");
            report.AppendLine("GENERAL DATA");
            report.AppendLine($"Total Groups: {groups.Length}");
            report.AppendLine($"Service Building Count: {serviceBuildingCount}");
            report.AppendLine($"Assigned Building Count: {assignedServiceBuildingCount}");
            report.AppendLine($"Total Districts Count: {districtCount}");

            report.AppendLine();
            report.AppendLine("GROUP STRUCTURE");
            for (int i = 0; i < groups.Length; i++)
            {
                Entity group = groups[i];
                DistrictGroupData groupData = EntityManager.GetComponentData<DistrictGroupData>(group);
                string groupName = groupData.m_Name.ToString();
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
                using NativeArray<Entity> buildings = GetAssignedBuildings(group, Allocator.Temp);

                report.AppendLine($"Group {i + 1}: {groupName} ({group})");
                report.AppendLine($"  Type: {groupData.m_Type}");

                report.AppendLine($"  Districts: {members.Length}");
                foreach (DistrictGroupMember member in members)
                {
                    string districtName = EntityManager.Exists(member.m_District)
                        ? m_NameSystem.GetRenderedLabelName(member.m_District)
                        : "<missing>";
                    report.AppendLine($"    {districtName} ({member.m_District})");
                }

                report.AppendLine($"  Assigned Buildings: {buildings.Length}");
                foreach (Entity building in buildings)
                {
                    string buildingName = m_NameSystem.GetRenderedLabelName(building);
                    GroupServiceType buildingType = DetectBuildingServiceType(building);
                    report.AppendLine($"    {buildingName} ({building}, Type: {buildingType})");
                }
            }

            report.AppendLine();
            report.AppendLine("DANGLING ASSIGNMENTS");
            using NativeArray<Entity> allAssignedBuildings = m_AssignmentQuery.ToEntityArray(Allocator.Temp);
            int danglingCount = 0;
            foreach (Entity building in allAssignedBuildings)
            {
                Entity group = EntityManager.GetComponentData<DistrictGroupAssignment>(building).m_Group;
                if (group == Entity.Null
                    || (EntityManager.Exists(group) && EntityManager.HasComponent<DistrictGroupData>(group)))
                {
                    continue;
                }
                danglingCount++;
                string buildingName = m_NameSystem.GetRenderedLabelName(building);
                report.AppendLine($"  {buildingName} ({building}) -> missing group ({group})");
            }
            if (danglingCount == 0)
            {
                report.AppendLine("None found.");
            }

            report.AppendLine("=== END DATA DUMP ===");

            Mod.log.Info(report.ToString());
        }
    }
}

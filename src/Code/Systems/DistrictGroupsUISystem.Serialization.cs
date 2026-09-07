using Colossal.Entities;
using Colossal.UI.Binding;
using Game.Buildings;
using Game.Prefabs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static DistrictGroups.EntityJson;

namespace DistrictGroups
{
    public partial class DistrictGroupsUISystem
    {
        private void WriteGroups(IJsonWriter writer)
        {
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            Dictionary<Entity, DistrictStats> districtStats = m_StatsSystem.GetDistrictStats();
            writer.ArrayBegin(groups.Length);
            foreach (Entity group in groups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
                DistrictStats groupStats = m_StatsSystem.GetGroupStats(group, districtStats);
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
                using NativeArray<Entity> assignedBuildings = m_GroupSystem.GetAssignedBuildings(group, Allocator.Temp);
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
                writer.Write(assignedBuildings.Length);
                WriteResidentStats(writer, groupStats);
                writer.PropertyName("members");
                writer.ArrayBegin(members.Length);
                foreach (DistrictGroupMember member in members)
                {
                    WriteDistrictMember(writer, member.m_District, districtStats);
                }
                writer.ArrayEnd();
                writer.PropertyName("buildings");
                writer.ArrayBegin(assignedBuildings.Length);
                foreach (Entity building in assignedBuildings)
                {
                    WriteAssignedBuilding(writer, building);
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
            WriteAssetName(writer, building);
            writer.TypeEnd();
        }

        // The building's asset name, as opposed to its instance name
        private void WriteAssetName(IJsonWriter writer, Entity building)
        {
            Entity prefab = EntityManager.HasComponent<PrefabRef>(building)
                ? EntityManager.GetComponentData<PrefabRef>(building).m_Prefab
                : Entity.Null;

            if (prefab == Entity.Null)
            {
                writer.PropertyName("assetNameId");
                writer.Write("");
                writer.PropertyName("assetName");
                writer.Write("");
                return;
            }

            m_PrefabUISystem.GetTitleAndDescription(prefab, out string titleId, out _);

            writer.PropertyName("assetNameId");
            writer.Write(titleId ?? "");
            writer.PropertyName("assetName");
            writer.Write(m_PrefabSystem.GetPrefabName(prefab));
        }

        // An assigned service building, carrying the per-building numbers its buildings row reads
        private void WriteAssignedBuilding(IJsonWriter writer, Entity building)
        {
            writer.TypeBegin("AssignedBuilding");
            writer.PropertyName("entity");
            WriteEntity(writer, building);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(building) ? m_NameSystem.GetRenderedLabelName(building) : "<missing>");
            writer.PropertyName("efficiency");
            writer.Write(GetEfficiencyPercent(building));
            writer.TypeEnd();
        }

        // A building's efficiency as the whole percent the game's own info panel shows, or kUnknownEfficiency when the game reports none for it.
        private int GetEfficiencyPercent(Entity building)
        {
            if (!EntityManager.TryGetBuffer(building, isReadOnly: true, out DynamicBuffer<Efficiency> efficiencies))
            {
                return kUnknownEfficiency;
            }

            float efficiency = 1f;
            foreach (Efficiency factor in efficiencies)
            {
                efficiency *= math.max(0f, factor.m_Efficiency);
            }

            // Anything still running reads as at least 1%, the same floor the info panel puts under it.
            return efficiency > 0f ? math.max(1, (int)math.round(100f * efficiency)) : 0;
        }

        // A member district, carrying the per-district numbers its overview row reads
        private void WriteDistrictMember(IJsonWriter writer, Entity entity, Dictionary<Entity, DistrictStats> districtStats)
        {
            districtStats.TryGetValue(entity, out DistrictStats stats);

            writer.TypeBegin("DistrictMember");
            writer.PropertyName("entity");
            WriteEntity(writer, entity);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(entity) ? m_NameSystem.GetRenderedLabelName(entity) : "<missing>");
            WriteResidentStats(writer, stats);
            writer.TypeEnd();
        }

        /*
            Happiness and wealth go over as the ordinal of the band the average lands in rather than the raw
            average, because bucketing wealth needs a game parameter singleton the UI cannot reach, and the
            panel only ever shows the band's name anyway. DistrictStatsSystem.kNoThreshold means the district
            or group had no residents to average.
        */
        private void WriteResidentStats(IJsonWriter writer, DistrictStats stats)
        {
            writer.PropertyName("population");
            writer.Write(stats.m_Population);
            writer.PropertyName("happiness");
            writer.Write(DistrictStatsSystem.GetHappinessThreshold(stats));
            writer.PropertyName("wealth");
            writer.Write(m_StatsSystem.GetWealthThreshold(stats));
        }
    }
}

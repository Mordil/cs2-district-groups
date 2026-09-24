using Colossal.UI.Binding;
using Game.Buildings;
using Game.Policies;
using Game.Prefabs;
using System;
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
            m_StatsSystem.RequestStats();
            DistrictStatsReader reader = m_StatsSystem.GetStatsReader();
            UpdateBuildingLookups();
            CollectAssignedBuildings();

            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            writer.ArrayBegin(groups.Length);
            foreach (Entity group in groups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
                DynamicBuffer<DistrictGroupMember> members =
                    EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);

                writer.TypeBegin("Group");
                writer.PropertyName("entity");
                WriteEntity(writer, group);
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.PropertyName("color");
                writer.Write(data.m_Color);
                WriteResidentStats(writer, SumMemberStats(members), reader);
                writer.PropertyName("members");
                writer.ArrayBegin(members.Length);
                foreach (DistrictGroupMember member in members)
                {
                    WriteDistrictMember(writer, member.m_District, reader);
                }
                writer.ArrayEnd();
                writer.PropertyName("buildings");
                WriteAssignedBuildings(writer, group);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        // The group's member districts added together, so its averages are population-weighted.
        private DistrictStats SumMemberStats(DynamicBuffer<DistrictGroupMember> members)
        {
            DistrictStats total = default;
            foreach (DistrictGroupMember member in members)
            {
                if (m_StatsSystem.TryGetDistrictStats(member.m_District, out DistrictStats memberStats))
                {
                    total.Add(memberStats);
                }
            }
            return total;
        }

        // Buckets every assigned building by the group it belongs to in a single pass.
        private void CollectAssignedBuildings()
        {
            foreach (KeyValuePair<Entity, List<Entity>> entry in m_BuildingsByGroup)
            {
                entry.Value.Clear();
            }

            using NativeArray<Entity> buildings = m_AssignmentQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<DistrictGroupAssignment> assignments =
                m_AssignmentQuery.ToComponentDataArray<DistrictGroupAssignment>(Allocator.Temp);

            for (int i = 0; i < buildings.Length; i++)
            {
                Entity group = assignments[i].m_Group;
                if (!m_BuildingsByGroup.TryGetValue(group, out List<Entity> assigned))
                {
                    assigned = new List<Entity>();
                    m_BuildingsByGroup.Add(group, assigned);
                }
                assigned.Add(buildings[i]);
            }
        }

        private void WriteAssignedBuildings(IJsonWriter writer, Entity group)
        {
            if (!m_BuildingsByGroup.TryGetValue(group, out List<Entity> buildings))
            {
                writer.ArrayBegin(0);
                writer.ArrayEnd();
                return;
            }

            writer.ArrayBegin(buildings.Count);
            foreach (Entity building in buildings)
            {
                WriteAssignedBuilding(writer, building);
            }
            writer.ArrayEnd();
        }

        private void WriteGroupPolicies(IJsonWriter writer)
        {
            IReadOnlyList<DistrictPolicy> policies = m_PolicySystem.Policies;
            m_DistrictPolicies.Update(this);

            CollectPolicyDistricts(m_GroupSystem.FocusedGroup);

            writer.ArrayBegin(policies.Count);
            foreach (DistrictPolicy policy in policies)
            {
                writer.TypeBegin("GroupPolicy");
                writer.PropertyName("entity");
                WriteEntity(writer, policy.m_Policy);
                writer.PropertyName("id");
                writer.Write(policy.m_Id);
                writer.PropertyName("icon");
                writer.Write(policy.m_Icon);
                writer.PropertyName("slider");
                WritePolicySlider(writer, policy);
                writer.PropertyName("districts");
                WritePolicyDistricts(writer, policy);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void WritePolicySlider(IJsonWriter writer, DistrictPolicy policy)
        {
            if (!policy.m_HasSlider)
            {
                writer.WriteNull();
                return;
            }

            PolicySliderData slider = policy.m_Slider;
            writer.TypeBegin("PolicySlider");
            writer.PropertyName("min");
            writer.Write(slider.m_Range.min);
            writer.PropertyName("max");
            writer.Write(slider.m_Range.max);
            writer.PropertyName("default");
            writer.Write(slider.m_Default);
            writer.PropertyName("step");
            writer.Write(slider.m_Step);
            // The same unit names the game's own policy sliders send, so the UI can format with them directly.
            writer.PropertyName("unit");
            writer.Write(Enum.GetName(typeof(PolicySliderUnit), (PolicySliderUnit)slider.m_Unit) ?? "");
            writer.TypeEnd();
        }

        private void CollectPolicyDistricts(Entity group)
        {
            /*
                Every policy in the payload lists the same member districts,
                so resolving a district's name and its policy buffer inside the policy loop would redo that work.
            */

            m_PolicyDistricts.Clear();

            if (!EntityManager.HasBuffer<DistrictGroupMember>(group))
            {
                return;
            }

            DynamicBuffer<DistrictGroupMember> members =
                EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
            foreach (DistrictGroupMember member in members)
            {
                string name = EntityManager.Exists(member.m_District)
                    ? m_NameSystem.GetRenderedLabelName(member.m_District)
                    : "<missing>";
                // A district with no policy buffer leaves an uncreated one behind, which the state read already handles.
                m_DistrictPolicies.TryGetBuffer(member.m_District, out DynamicBuffer<Policy> policies);
                m_PolicyDistricts.Add(new PolicyDistrict(member.m_District, name, policies));
            }
        }

        private void WritePolicyDistricts(IJsonWriter writer, DistrictPolicy policy)
        {
            float defaultValue = policy.DefaultValue;
            writer.ArrayBegin(m_PolicyDistricts.Count);
            foreach (PolicyDistrict district in m_PolicyDistricts)
            {
                DistrictPolicyState state = DistrictGroupPolicySystem.GetDistrictState(district.m_Policies, policy.m_Policy, defaultValue);
                writer.TypeBegin("DistrictPolicyState");
                writer.PropertyName("entity");
                WriteEntity(writer, district.m_District);
                writer.PropertyName("name");
                writer.Write(district.m_Name);
                writer.PropertyName("active");
                writer.Write(state.m_Active);
                writer.PropertyName("value");
                writer.Write(state.m_Value);
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

        // An assigned service building, carrying the per-building numbers its buildings row reads.
        //
        // Its service type is settled first, so every figure is read off the one facility component that type names.
        private void WriteAssignedBuilding(IJsonWriter writer, Entity building)
        {
            m_BuildingPrefabs.TryGetComponent(building, out PrefabRef prefabRef);
            Facility facility = new Facility
            {
                m_Building = building,
                m_Prefab = prefabRef.m_Prefab,
                m_Type = m_GroupSystem.DetectServiceType(prefabRef.m_Prefab),
            };
            m_InstalledUpgrades.TryGetBuffer(building, out facility.m_Upgrades);
            GetOccupancy(facility, out int occupants, out int capacity);

            writer.TypeBegin("AssignedBuilding");
            writer.PropertyName("entity");
            WriteEntity(writer, building);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(building) ? m_NameSystem.GetRenderedLabelName(building) : "<missing>");
            writer.PropertyName("type");
            writer.Write((int)facility.m_Type);
            writer.PropertyName("efficiency");
            writer.Write(GetEfficiencyPercent(building));
            writer.PropertyName("occupants");
            writer.Write(occupants);
            writer.PropertyName("capacity");
            writer.Write(capacity);
            writer.TypeEnd();
        }

        // How full a building's own places are, with installed upgrades folded in.
        //
        // Both read kNoValue for a building with no such places to report.
        private void GetOccupancy(Facility facility, out int occupants, out int capacity)
        {
            occupants = kNoValue;
            capacity = kNoValue;

            switch (facility.m_Type)
            {
                // A police group holds both stations and prisons, and whichever the building is, its own capacity answers for it.
                case GroupServiceType.Police:
                    if (TryGetData(facility, ref m_PoliceStations, out PoliceStationData station))
                    {
                        occupants = BufferLength(facility.m_Building, ref m_Occupants);
                        capacity = station.m_JailCapacity;
                        return;
                    }

                    if (TryGetData(facility, ref m_Prisons, out PrisonData prison))
                    {
                        occupants = BufferLength(facility.m_Building, ref m_Occupants);
                        capacity = prison.m_PrisonerCapacity;
                    }

                    return;

                // A fire group holds stations as well as shelters, and only a shelter holds anybody.
                case GroupServiceType.Fire:
                    if (TryGetData(facility, ref m_EmergencyShelters, out EmergencyShelterData shelter))
                    {
                        occupants = BufferLength(facility.m_Building, ref m_Occupants);
                        capacity = shelter.m_ShelterCapacity;
                    }

                    return;
            }
        }

        // One facility's own prefab data, with whatever its installed upgrades change about it folded in.
        private bool TryGetData<T>(Facility facility, ref ComponentLookup<T> facilities, out T data)
            where T : unmanaged, IComponentData, ICombineData<T>
        {
            if (!facilities.TryGetComponent(facility.m_Prefab, out data))
            {
                data = default;
                return false;
            }

            if (facility.m_Upgrades.IsCreated && facility.m_Upgrades.Length != 0)
            {
                UpgradeUtils.CombineStats(ref data, facility.m_Upgrades, ref m_BuildingPrefabs, ref facilities);
            }

            return true;
        }

        // How many places of a kind a building has taken, as the length of the buffer it holds them in.
        private int BufferLength<T>(Entity building, ref BufferLookup<T> holders)
            where T : unmanaged, IBufferElementData =>
            holders.TryGetBuffer(building, out DynamicBuffer<T> held) ? held.Length : 0;

        // A building's efficiency as the whole percent the game's own info panel shows, or kNoValue when the game reports none for it.
        private int GetEfficiencyPercent(Entity building)
        {
            if (!m_BuildingEfficiencies.TryGetBuffer(building, out DynamicBuffer<Efficiency> efficiencies))
            {
                return kNoValue;
            }

            float efficiency = 1f;
            foreach (Efficiency factor in efficiencies)
            {
                efficiency *= math.max(0f, factor.m_Efficiency);
            }

            // Anything still running reads as at least 1%, the same floor the info panel puts under it.
            return efficiency > 0f ? math.max(1, (int)math.round(100f * efficiency)) : 0;
        }

        // Points every lookup the building rows read through at the current frame's data.
        private void UpdateBuildingLookups()
        {
            m_BuildingPrefabs.Update(this);
            m_InstalledUpgrades.Update(this);
            m_BuildingEfficiencies.Update(this);
            m_Occupants.Update(this);
            m_PoliceStations.Update(this);
            m_Prisons.Update(this);
            m_EmergencyShelters.Update(this);
        }

        // A member district, carrying the per-district numbers its overview row reads
        private void WriteDistrictMember(IJsonWriter writer, Entity entity, DistrictStatsReader reader)
        {
            m_StatsSystem.TryGetDistrictStats(entity, out DistrictStats stats);

            writer.TypeBegin("DistrictMember");
            writer.PropertyName("entity");
            WriteEntity(writer, entity);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(entity) ? m_NameSystem.GetRenderedLabelName(entity) : "<missing>");
            WriteResidentStats(writer, stats, reader);
            writer.TypeEnd();
        }

        // Writes all the stats from the reader into the JSON data buffer.
        // Stats with `kNoValue` means the district or group had nothing to report for that figure.
        private void WriteResidentStats(IJsonWriter writer, DistrictStats stats, DistrictStatsReader reader)
        {
            writer.PropertyName("population");
            writer.Write(stats.m_Population);
            writer.PropertyName("happiness");
            writer.Write(reader.Happiness(stats));
            writer.PropertyName("wealth");
            writer.Write(reader.Wealth(stats));
            writer.PropertyName("income");
            writer.Write(reader.Income(stats));
            writer.PropertyName("crimeChance");
            writer.Write(reader.CrimeChance(stats));
            writer.PropertyName("fireRisk");
            writer.Write(reader.FireRisk(stats));
        }
    }
}

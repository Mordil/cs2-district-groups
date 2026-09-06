using Colossal.Entities;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Economy;
using Game.Prefabs;
using Game.UI.InGame;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    /*
        Per-district resident aggregation.

        Nothing in Cities: Skylines II is stored per district, so every figure here is the same sum the
        vanilla infoview panels take over the whole city, filtered down to one district by the building's
        CurrentDistrict link. The filters and divisors mirror AverageHappinessSection and
        WealthInfoviewUISystem so our numbers agree with the panels players already read.
    */
    public partial class DistrictGroupSystem
    {
        // Marks the cached per-district totals for a fresh sweep on the next read.
        public void InvalidateDistrictStats()
        {
            m_DistrictStatsStale = true;
        }

        // District -> resident totals, swept from every residential building the district holds.
        public Dictionary<Entity, DistrictStats> GetDistrictStats()
        {
            if (!m_DistrictStatsStale)
            {
                return m_CachedDistrictStats;
            }
            m_DistrictStatsStale = false;

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Dictionary<Entity, DistrictStats> stats = new Dictionary<Entity, DistrictStats>();
            using NativeArray<Entity> buildings = m_ResidentialBuildingQuery.ToEntityArray(Allocator.Temp);
            foreach (Entity building in buildings)
            {
                Entity district = EntityManager.GetComponentData<CurrentDistrict>(building).m_District;
                if (district == Entity.Null)
                {
                    continue;
                }
                stats.TryGetValue(district, out DistrictStats districtStats);
                AccumulateBuilding(building, ref districtStats);
                stats[district] = districtStats;
            }
            m_CachedDistrictStats = stats;

            Mod.log.Debug($"Swept district resident stats; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} " +
                $"building_count:{buildings.Length} district_count:{stats.Count}");
            return m_CachedDistrictStats;
        }

        // The group's member districts added together, so its averages are population-weighted.
        public DistrictStats GetGroupStats(Entity group, Dictionary<Entity, DistrictStats> districtStats)
        {
            DistrictStats stats = default;
            using NativeArray<Entity> districts = GetValidMemberDistricts(group, Allocator.Temp);
            foreach (Entity district in districts)
            {
                if (districtStats.TryGetValue(district, out DistrictStats memberStats))
                {
                    stats.Add(memberStats);
                }
            }
            return stats;
        }

        // Which happiness band the average resident falls in, as a Game.Citizens.CitizenHappiness
        // ordinal, or kNoThreshold when there are no living residents to average.
        public static int GetHappinessThreshold(DistrictStats stats)
        {
            if (stats.m_LivingResidentCount == 0)
            {
                return kNoThreshold;
            }
            return (int)CitizenUtils.GetHappinessKey(stats.m_HappinessSum / stats.m_LivingResidentCount);
        }

        // Which wealth band the average household falls in, as a Game.UI.InGame.HouseholdWealthKey
        // ordinal, or kNoThreshold when there are no resident households to average.
        public int GetWealthThreshold(DistrictStats stats)
        {
            if (stats.m_HouseholdCount == 0 || m_CitizenHappinessParameterQuery.IsEmptyIgnoreFilter)
            {
                return kNoThreshold;
            }
            CitizenHappinessParameterData parameters = m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>();
            int averageWealth = (int)(stats.m_WealthSum / stats.m_HouseholdCount);
            return (int)CitizenUIUtils.GetHouseholdWealthKey(averageWealth, parameters);
        }

        // Adds one residential building's renter households to the totals.
        private void AccumulateBuilding(Entity building, ref DistrictStats stats)
        {
            if (!EntityManager.TryGetBuffer(building, true, out DynamicBuffer<Renter> renters))
            {
                return;
            }
            foreach (Renter renter in renters)
            {
                AccumulateHousehold(renter.m_Renter, ref stats);
            }
        }

        /*
            Vanilla's two sweeps disagree on which households they take, so each accumulator keeps its own
            filter: happiness counts every renter household's living citizens, while the wealth average
            leaves out tourists, commuters and households already on their way out of the city.
        */
        private void AccumulateHousehold(Entity household, ref DistrictStats stats)
        {
            bool isHousehold = EntityManager.TryGetComponent(household, out Household householdData);
            bool hasResidents = EntityManager.TryGetBuffer(household, true, out DynamicBuffer<HouseholdCitizen> residents);
            if (!isHousehold || !hasResidents)
            {
                return;
            }

            stats.m_Population += residents.Length;
            foreach (HouseholdCitizen resident in residents)
            {
                bool isCitizen = EntityManager.TryGetComponent(resident.m_Citizen, out Citizen citizen);
                if (!isCitizen || CitizenUtils.IsDead(EntityManager, resident.m_Citizen))
                {
                    continue;
                }
                stats.m_HappinessSum += citizen.Happiness;
                stats.m_LivingResidentCount++;
            }

            bool isTemporaryResident = EntityManager.HasComponent<TouristHousehold>(household)
                || EntityManager.HasComponent<CommuterHousehold>(household)
                || EntityManager.HasComponent<MovingAway>(household);
            bool hasWealth = EntityManager.TryGetBuffer(household, true, out DynamicBuffer<Resources> resources);
            if (isTemporaryResident || !hasWealth)
            {
                return;
            }
            stats.m_WealthSum += EconomyUtils.GetHouseholdTotalWealth(householdData, resources);
            stats.m_HouseholdCount++;
        }
    }
}

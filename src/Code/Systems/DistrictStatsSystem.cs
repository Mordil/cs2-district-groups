using Game;
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
using Unity.Jobs;

using Purpose = Colossal.Serialization.Entities.Purpose;

namespace DistrictGroups
{
    /*
        Per-district aggregation for the panels to read.

        Nothing in Cities: Skylines II is stored per district, so every figure here is the same sum the
        vanilla infoview panels take over the whole city, filtered down to one district by the building's
        CurrentDistrict link.

        Sweeping is this system's whole job; DistrictGroupSystem owns the groups and is only asked which
        districts belong to one.

        The sweep runs as a parallel job because its cost is a fixed number of entity hops: a building's
        renters, their households, their citizens. Readers never wait for it: GetDistrictStats hands back
        the last completed sweep, and StatsVersion tells the UI when a newer one has landed.
    */
    public partial class DistrictStatsSystem : GameSystemBase
    {
        // What a threshold reads as when the district or group has no residents to average.
        public const int kNoThreshold = -1;

        private const int kSweepBatchSize = 16;

        // Residential buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_ResidentialBuildingQuery;
        // Holds the wealth thresholds the average household wealth is bucketed against.
        private EntityQuery m_CitizenHappinessParameterQuery;

        /*
            Every hop the resident sweep makes off a building - renters, their households, their citizens -
            goes through one of these rather than through EntityManager.

            EntityManager charges every call for work a sweep only needs once: it resolves the component's
            index inside the entity's archetype from scratch, and completes any job writing that type. A
            lookup does neither - it carries a LookupCache that skips the resolution whenever consecutive
            entities share an archetype, which households and citizens overwhelmingly do.
        */
        private BufferLookup<Renter> m_Renters;
        private BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
        private BufferLookup<Resources> m_Resources;
        private ComponentLookup<Household> m_Households;
        private ComponentLookup<Citizen> m_Citizens;
        private ComponentLookup<HealthProblem> m_HealthProblems;
        private ComponentLookup<TouristHousehold> m_TouristHouseholds;
        private ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
        private ComponentLookup<MovingAway> m_MovingAwayHouseholds;

        private Dictionary<Entity, DistrictStats> m_CachedDistrictStats = new Dictionary<Entity, DistrictStats>();
        private bool m_DistrictStatsStale = true;
        // Which districts the cached totals were swept for, so a membership change can retire them.
        private HashSet<Entity> m_DistrictStatsScope = new HashSet<Entity>();
        // Whether anything has read the totals since the last sweep was scheduled.
        private bool m_StatsRequested;
        private int m_LastSeenCompositionVersion = -1;

        /*
            The in-scope buildings and the district each one belongs to, filtered on the main thread so the
            job needs no scope test of its own, plus one result slot per building.

            Every slot is written by exactly one iteration and folded together afterwards, which is what lets
            the sweep run parallel without per-thread accumulators or atomics.
        */
        private NativeList<Entity> m_SweepBuildings;
        private NativeList<Entity> m_SweepDistricts;
        private NativeList<DistrictStats> m_SweepResults;
        private JobHandle m_SweepHandle;
        private bool m_SweepInFlight;

        // Bumped whenever a sweep lands, so the UI knows to re-read totals it has already written once.
        public int StatsVersion { get; private set; }

        private DistrictGroupSystem m_GroupSystem;

        private int m_SweepBuildingCount;
        private double m_SweepScheduleMs;
        private readonly System.Diagnostics.Stopwatch m_SweepClock = new System.Diagnostics.Stopwatch();

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();

            m_ResidentialBuildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());

            m_Renters = GetBufferLookup<Renter>(true);
            m_HouseholdCitizens = GetBufferLookup<HouseholdCitizen>(true);
            m_Resources = GetBufferLookup<Resources>(true);
            m_Households = GetComponentLookup<Household>(true);
            m_Citizens = GetComponentLookup<Citizen>(true);
            m_HealthProblems = GetComponentLookup<HealthProblem>(true);
            m_TouristHouseholds = GetComponentLookup<TouristHousehold>(true);
            m_CommuterHouseholds = GetComponentLookup<CommuterHousehold>(true);
            m_MovingAwayHouseholds = GetComponentLookup<MovingAway>(true);

            m_SweepBuildings = new NativeList<Entity>(Allocator.Persistent);
            m_SweepDistricts = new NativeList<Entity>(Allocator.Persistent);
            m_SweepResults = new NativeList<DistrictStats>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_SweepHandle.Complete();
            m_SweepBuildings.Dispose();
            m_SweepDistricts.Dispose();
            m_SweepResults.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (m_SweepInFlight)
            {
                PublishSweep();
                return;
            }

            int compositionVersion = m_GroupSystem.GroupCompositionVersion;
            bool compositionChanged = compositionVersion != m_LastSeenCompositionVersion;
            if (!m_StatsRequested || (!m_DistrictStatsStale && !compositionChanged))
            {
                return;
            }
            m_LastSeenCompositionVersion = compositionVersion;

            HashSet<Entity> scope = GetGroupedDistricts();
            bool scopeChanged = !scope.SetEquals(m_DistrictStatsScope);
            if (!m_DistrictStatsStale && !scopeChanged)
            {
                return;
            }

            ScheduleSweep(scope);
        }

        // Entity ids are only meaningful within one city, so nothing swept from the outgoing one may be reused.
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            m_SweepHandle.Complete();
            m_SweepInFlight = false;
            m_CachedDistrictStats.Clear();
            m_DistrictStatsScope.Clear();
            m_DistrictStatsStale = true;
            m_LastSeenCompositionVersion = -1;
        }

        // Marks the cached per-district totals for a fresh sweep on the next update.
        public void InvalidateDistrictStats()
        {
            m_DistrictStatsStale = true;
        }

        // District -> resident totals from the last completed sweep, for every district in a group.
        public Dictionary<Entity, DistrictStats> GetDistrictStats()
        {
            m_StatsRequested = true;
            return m_CachedDistrictStats;
        }

        // The group's member districts added together, so its averages are population-weighted.
        public DistrictStats GetGroupStats(Entity group, Dictionary<Entity, DistrictStats> districtStats)
        {
            DistrictStats stats = default;
            using NativeArray<Entity> districts = m_GroupSystem.GetValidMemberDistricts(group, Allocator.Temp);
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

        // Hands the in-scope buildings to a parallel sweep and leaves it running.
        private void ScheduleSweep(HashSet<Entity> scope)
        {
            m_DistrictStatsStale = false;
            m_StatsRequested = false;
            m_DistrictStatsScope = scope;
            m_SweepClock.Restart();
            UpdateStatsLookups();

            using NativeArray<Entity> buildings = m_ResidentialBuildingQuery.ToEntityArray(Allocator.Temp);
            using NativeArray<CurrentDistrict> districts =
                m_ResidentialBuildingQuery.ToComponentDataArray<CurrentDistrict>(Allocator.Temp);

            m_SweepBuildings.Clear();
            m_SweepDistricts.Clear();
            for (int i = 0; i < buildings.Length; i++)
            {
                Entity district = districts[i].m_District;
                if (!scope.Contains(district))
                {
                    continue;
                }
                m_SweepBuildings.Add(buildings[i]);
                m_SweepDistricts.Add(district);
            }
            m_SweepResults.Resize(m_SweepBuildings.Length, NativeArrayOptions.UninitializedMemory);
            m_SweepBuildingCount = buildings.Length;

            SweepResidentsJob job = new SweepResidentsJob
            {
                m_Buildings = m_SweepBuildings.AsArray(),
                m_Renters = m_Renters,
                m_HouseholdCitizens = m_HouseholdCitizens,
                m_Resources = m_Resources,
                m_Households = m_Households,
                m_Citizens = m_Citizens,
                m_HealthProblems = m_HealthProblems,
                m_TouristHouseholds = m_TouristHouseholds,
                m_CommuterHouseholds = m_CommuterHouseholds,
                m_MovingAwayHouseholds = m_MovingAwayHouseholds,
                m_Results = m_SweepResults.AsArray(),
            };
            m_SweepHandle = job.Schedule(m_SweepBuildings.Length, kSweepBatchSize, Dependency);
            Dependency = m_SweepHandle;
            m_SweepInFlight = true;
            m_SweepScheduleMs = m_SweepClock.Elapsed.TotalMilliseconds;
        }

        // Folds the finished per-building results into the per-district totals readers see.
        private void PublishSweep()
        {
            double scheduledMs = m_SweepClock.Elapsed.TotalMilliseconds;

            bool finishedUnwatched = m_SweepHandle.IsCompleted;
            m_SweepHandle.Complete();
            m_SweepInFlight = false;
            double blockMs = m_SweepClock.Elapsed.TotalMilliseconds - scheduledMs;

            Dictionary<Entity, DistrictStats> stats = new Dictionary<Entity, DistrictStats>();
            DistrictStats total = default;
            for (int i = 0; i < m_SweepDistricts.Length; i++)
            {
                Entity district = m_SweepDistricts[i];
                stats.TryGetValue(district, out DistrictStats districtStats);
                districtStats.Add(m_SweepResults[i]);
                stats[district] = districtStats;
                total.Add(m_SweepResults[i]);
            }
            m_CachedDistrictStats = stats;
            StatsVersion++;

            double latencyMs = m_SweepClock.Elapsed.TotalMilliseconds;
            double publishMs = latencyMs - scheduledMs - blockMs;
            Mod.log.Debug($"Swept district resident stats; main_thread_ms:{m_SweepScheduleMs + blockMs + publishMs:F3} " +
                $"schedule_ms:{m_SweepScheduleMs:F3} block_ms:{blockMs:F3} publish_ms:{publishMs:F3} " +
                $"latency_ms:{latencyMs:F3} finished_early:{finishedUnwatched} building_count:{m_SweepBuildingCount} " +
                $"swept_building_count:{m_SweepBuildings.Length} scope_district_count:{m_DistrictStatsScope.Count} " +
                $"district_count:{stats.Count} population:{total.m_Population} household_count:{total.m_HouseholdCount}");
        }

        // Every district that belongs to a group, which is the whole scope the resident sweep covers.
        private HashSet<Entity> GetGroupedDistricts()
        {
            HashSet<Entity> scope = new HashSet<Entity>();
            using NativeArray<Entity> groups = m_GroupSystem.GetGroups(Allocator.Temp);
            foreach (Entity group in groups)
            {
                using NativeArray<Entity> members = m_GroupSystem.GetValidMemberDistricts(group, Allocator.Temp);
                foreach (Entity district in members)
                {
                    scope.Add(district);
                }
            }
            return scope;
        }

        // Points every lookup the sweep hops through at the current frame's data.
        private void UpdateStatsLookups()
        {
            m_Renters.Update(this);
            m_HouseholdCitizens.Update(this);
            m_Resources.Update(this);
            m_Households.Update(this);
            m_Citizens.Update(this);
            m_HealthProblems.Update(this);
            m_TouristHouseholds.Update(this);
            m_CommuterHouseholds.Update(this);
            m_MovingAwayHouseholds.Update(this);
        }

        // One in-scope building's residents per iteration, summed into that building's own result slot.
        private struct SweepResidentsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> m_Buildings;
            [ReadOnly] public BufferLookup<Renter> m_Renters;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly] public BufferLookup<Resources> m_Resources;
            [ReadOnly] public ComponentLookup<Household> m_Households;
            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly] public ComponentLookup<TouristHousehold> m_TouristHouseholds;
            [ReadOnly] public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAwayHouseholds;
            public NativeArray<DistrictStats> m_Results;

            public void Execute(int index)
            {
                DistrictStats stats = default;
                AccumulateBuilding(m_Buildings[index], ref stats);
                m_Results[index] = stats;
            }

            // Adds one residential building's renter households to the totals.
            private void AccumulateBuilding(Entity building, ref DistrictStats stats)
            {
                if (!m_Renters.TryGetBuffer(building, out DynamicBuffer<Renter> renters))
                {
                    return;
                }
                foreach (Renter renter in renters)
                {
                    AccumulateHousehold(renter.m_Renter, ref stats);
                }
            }

            private void AccumulateHousehold(Entity household, ref DistrictStats stats)
            {
                /*
                    Vanilla's two sweeps disagree on which households they take, so each accumulator keeps its
                    own filter: happiness counts every renter household's living citizens, while the wealth
                    average leaves out tourists, commuters and households already on their way out of the city.
                */

                bool isHousehold = m_Households.TryGetComponent(household, out Household householdData);
                bool hasResidents = m_HouseholdCitizens.TryGetBuffer(household, out DynamicBuffer<HouseholdCitizen> residents);
                if (!isHousehold || !hasResidents)
                {
                    return;
                }

                stats.m_Population += residents.Length;
                foreach (HouseholdCitizen resident in residents)
                {
                    bool isCitizen = m_Citizens.TryGetComponent(resident.m_Citizen, out Citizen citizen);
                    if (!isCitizen || CitizenUtils.IsDead(resident.m_Citizen, ref m_HealthProblems))
                    {
                        continue;
                    }
                    stats.m_HappinessSum += citizen.Happiness;
                    stats.m_LivingResidentCount++;
                }

                bool isTemporaryResident = m_TouristHouseholds.HasComponent(household)
                    || m_CommuterHouseholds.HasComponent(household)
                    || m_MovingAwayHouseholds.HasComponent(household);
                bool hasWealth = m_Resources.TryGetBuffer(household, out DynamicBuffer<Resources> resources);
                if (isTemporaryResident || !hasWealth)
                {
                    return;
                }
                stats.m_WealthSum += EconomyUtils.GetHouseholdTotalWealth(householdData, resources);
                stats.m_HouseholdCount++;
            }
        }
    }
}

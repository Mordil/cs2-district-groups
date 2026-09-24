using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Economy;
using Game.Prefabs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using Purpose = Colossal.Serialization.Entities.Purpose;

namespace DistrictGroups
{
    /// <summary>
    /// Per-district aggregation data for the UI to display.
    /// 
    /// Data is not stored in districts, so we do a reverse association by querying all entities in the city,
    /// and matching them to their currentDistrict assignment.
    /// 
    /// As much as possible, query and data aggregation happens in Jobs to keep the main thread clear.
    /// </summary>
    public partial class DistrictStatsSystem : GameSystemBase
    {
        private const int kSweepBatchSize = 16;

        // Residential buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_ResidentialBuildingQuery;
        // Crime-producing buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_CrimeProducerQuery;
        // Flammable buildings, keyed to a district via CurrentDistrict; excludes fire stations and buildings already
        // on fire, which contribute no fire risk of their own.
        private EntityQuery m_FlammableBuildingQuery;
        // Holds the wealth thresholds the average household wealth is bucketed against.
        private EntityQuery m_CitizenHappinessParameterQuery;
        // Holds the crime accumulation ceiling a district's average crime is read as a share of.
        private EntityQuery m_PoliceConfigurationQuery;

        /*
            Every hop a sweep makes off a building - renters, their households, their citizens - goes
            through one of these rather than through EntityManager.
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
        // What a district's own membership is tested with while the scope is being collected.
        private ComponentLookup<District> m_DistrictAreas;
        private ComponentLookup<Game.Common.Deleted> m_DeletedEntities;
        private BufferLookup<DistrictGroupMember> m_GroupMembers;

        private ComponentLookup<Game.Buildings.CrimeProducer> m_CrimeProducers;

        /*
            All the items needed to recreate the fire hazard algorithm since it's not fully exposed by the game.
        */
        private ComponentLookup<Building> m_Buildings;
        private ComponentLookup<PrefabRef> m_BuildingPrefabs;
        private ComponentLookup<DestructibleObjectData> m_PrefabFireHazards;
        private ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildings;
        private ComponentLookup<ZonePropertiesData> m_PrefabZoneProperties;
        private ComponentLookup<Game.Objects.UnderConstruction> m_BuildingsUnderConstruction;
        private BufferLookup<Game.Net.ServiceCoverage> m_RoadServiceCoverages;
        private BufferLookup<DistrictModifier> m_DistrictModifiers;

        /*
            Every district that belongs to a group, and the dense slot its totals are accumulated into.

            A slot map rather than a dictionary keyed by entity, because a job can read it and because a
            sweep then adds into one array element instead of copying a whole DistrictStats in and out of
            a managed dictionary for every building it visits.
        */
        private NativeList<Entity> m_ScopeDistricts;
        private NativeHashMap<Entity, int> m_ScopeSlots;
        // Keeps a district out of the scope list twice over when it belongs to more than one group.
        private NativeHashSet<Entity> m_ScopeSeen;

        private NativeList<DistrictStats> m_SweepTotals;

        // The in-scope buildings each sweep visits, with the slot their district's totals belong to,
        // narrowed on the main thread so no job needs a scope test of its own.
        private NativeList<ScopedBuilding> m_ResidentHomes;
        private NativeList<DistrictStats> m_ResidentResults;
        private NativeList<ScopedBuilding> m_CrimeProducerBuildings;
        private NativeList<ScopedBuilding> m_FlammableBuildings;

        private NativeList<SumAndCount> m_CrimeTotals;
        private NativeList<SumAndCount> m_FireRiskTotals;

        private JobHandle m_SweepHandle;
        private bool m_SweepInFlight;

        // District -> totals from the last completed sweep.
        // Refilled in place at each publish, so a steady state of sweeps allocates nothing.
        private readonly Dictionary<Entity, DistrictStats> m_PublishedStats =
            new Dictionary<Entity, DistrictStats>();

        private bool m_DistrictStatsStale = true;
        // Whether anything has read the totals since the last sweep was scheduled.
        private bool m_StatsRequested;
        private int m_LastSeenCompositionVersion = -1;

        // Bumped whenever a sweep lands, so the UI knows to re-read totals it has already written once.
        public int StatsVersion { get; private set; }

        private DistrictGroupSystem m_GroupSystem;

        private int m_ResidentHomeCount;
        private double m_SweepScheduleMs;
        private readonly System.Diagnostics.Stopwatch m_SweepClock = new System.Diagnostics.Stopwatch();

        // One in-scope building and the slot its district's totals are accumulated into.
        private struct ScopedBuilding
        {
            public Entity m_Building;
            public int m_Slot;
        }

        private struct SumAndCount
        {
            public float m_Sum;
            public int m_Count;
        }

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
            m_CrimeProducerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Buildings.CrimeProducer>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_FlammableBuildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Game.Buildings.FireStation>(),
                ComponentType.Exclude<Game.Events.OnFire>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
            m_PoliceConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());

            m_Renters = GetBufferLookup<Renter>(true);
            m_HouseholdCitizens = GetBufferLookup<HouseholdCitizen>(true);
            m_Resources = GetBufferLookup<Resources>(true);
            m_Households = GetComponentLookup<Household>(true);
            m_Citizens = GetComponentLookup<Citizen>(true);
            m_HealthProblems = GetComponentLookup<HealthProblem>(true);
            m_TouristHouseholds = GetComponentLookup<TouristHousehold>(true);
            m_CommuterHouseholds = GetComponentLookup<CommuterHousehold>(true);
            m_MovingAwayHouseholds = GetComponentLookup<MovingAway>(true);
            m_DistrictAreas = GetComponentLookup<District>(true);
            m_DeletedEntities = GetComponentLookup<Game.Common.Deleted>(true);
            m_GroupMembers = GetBufferLookup<DistrictGroupMember>(true);

            m_CrimeProducers = GetComponentLookup<Game.Buildings.CrimeProducer>(true);

            m_Buildings = GetComponentLookup<Building>(true);
            m_BuildingPrefabs = GetComponentLookup<PrefabRef>(true);
            m_PrefabFireHazards = GetComponentLookup<DestructibleObjectData>(true);
            m_PrefabSpawnableBuildings = GetComponentLookup<SpawnableBuildingData>(true);
            m_PrefabZoneProperties = GetComponentLookup<ZonePropertiesData>(true);
            m_BuildingsUnderConstruction = GetComponentLookup<Game.Objects.UnderConstruction>(true);
            m_RoadServiceCoverages = GetBufferLookup<Game.Net.ServiceCoverage>(true);
            m_DistrictModifiers = GetBufferLookup<DistrictModifier>(true);

            m_ScopeDistricts = new NativeList<Entity>(Allocator.Persistent);
            m_ScopeSlots = new NativeHashMap<Entity, int>(16, Allocator.Persistent);
            m_ScopeSeen = new NativeHashSet<Entity>(16, Allocator.Persistent);
            m_SweepTotals = new NativeList<DistrictStats>(Allocator.Persistent);

            m_ResidentHomes = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_ResidentResults = new NativeList<DistrictStats>(Allocator.Persistent);
            m_CrimeProducerBuildings = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_FlammableBuildings = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_CrimeTotals = new NativeList<SumAndCount>(Allocator.Persistent);
            m_FireRiskTotals = new NativeList<SumAndCount>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_SweepHandle.Complete();
            m_ScopeDistricts.Dispose();
            m_ScopeSlots.Dispose();
            m_ScopeSeen.Dispose();
            m_SweepTotals.Dispose();
            m_ResidentHomes.Dispose();
            m_ResidentResults.Dispose();
            m_CrimeProducerBuildings.Dispose();
            m_FlammableBuildings.Dispose();
            m_CrimeTotals.Dispose();
            m_FireRiskTotals.Dispose();
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

            bool scopeChanged = CollectScope();
            if (!m_DistrictStatsStale && !scopeChanged)
            {
                return;
            }

            ScheduleSweep();
        }

        // Entity ids are only meaningful within one city, so nothing swept from the outgoing one may be reused.
        protected override void OnGamePreload(Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            m_SweepHandle.Complete();
            m_SweepInFlight = false;
            m_PublishedStats.Clear();
            m_ScopeDistricts.Clear();
            m_ScopeSlots.Clear();
            m_DistrictStatsStale = true;
            m_LastSeenCompositionVersion = -1;
        }

        // Marks the cached per-district totals for a fresh sweep on the next update.
        public void InvalidateDistrictStats()
        {
            m_DistrictStatsStale = true;
        }

        // Notes that something is about to read the totals, so a sweep keeps being scheduled for it.
        public void RequestStats()
        {
            m_StatsRequested = true;
        }

        // The last completed sweep's totals for one district, or false for a district it did not cover.
        public bool TryGetDistrictStats(Entity district, out DistrictStats stats) =>
            m_PublishedStats.TryGetValue(district, out stats);

        // The city-wide parameters the derived figures are measured against, gathered once per payload.
        public DistrictStatsReader GetStatsReader()
        {
            bool hasWealthBands = !m_CitizenHappinessParameterQuery.IsEmptyIgnoreFilter;
            CitizenHappinessParameterData wealthBands = hasWealthBands
                ? m_CitizenHappinessParameterQuery.GetSingleton<CitizenHappinessParameterData>()
                : default;
            float maxCrimeAccumulation = m_PoliceConfigurationQuery.IsEmptyIgnoreFilter
                ? 0f
                : m_PoliceConfigurationQuery.GetSingleton<PoliceConfigurationData>().m_MaxCrimeAccumulation;

            return new DistrictStatsReader(hasWealthBands, wealthBands, maxCrimeAccumulation);
        }

        /*
            Rebuilds the list of districts that belong to a group, and says whether it differs from the
            slots currently in force.

            Read off each group's own membership buffer rather than through DistrictGroupSystem, which
            hands back a fresh NativeArray per group - the scope is collected on every update that has
            been asked for stats, so it must cost no allocation at all.
        */
        private bool CollectScope()
        {
            int previousCount = m_ScopeSlots.Count;
            m_ScopeDistricts.Clear();
            m_ScopeSeen.Clear();
            m_GroupMembers.Update(this);
            m_DistrictAreas.Update(this);
            m_DeletedEntities.Update(this);

            using NativeArray<Entity> groups = m_GroupSystem.GetGroups(Allocator.Temp);
            foreach (Entity group in groups)
            {
                if (!m_GroupMembers.TryGetBuffer(group, out DynamicBuffer<DistrictGroupMember> members))
                {
                    continue;
                }

                foreach (DistrictGroupMember member in members)
                {
                    Entity district = member.m_District;
                    /*
                        A district the world has already dropped must never reach the sweep. Membership
                        pruning happens in DistrictGroupSyncSystem and on load, so a stale entry here is
                        only ever one this update has not seen pruned yet.
                    */
                    bool isLiveDistrict = m_DistrictAreas.HasComponent(district)
                        && !m_DeletedEntities.HasComponent(district);

                    if (!isLiveDistrict)
                    {
                        continue;
                    }

                    if (m_ScopeSeen.Add(district))
                    {
                        m_ScopeDistricts.Add(district);
                    }
                }
            }

            if (m_ScopeDistricts.Length != previousCount)
            {
                return true;
            }

            foreach (Entity district in m_ScopeDistricts)
            {
                if (!m_ScopeSlots.ContainsKey(district))
                {
                    return true;
                }
            }

            return false;
        }

        // Hands every sweep its in-scope buildings and leaves them running.
        private void ScheduleSweep()
        {
            m_DistrictStatsStale = false;
            m_StatsRequested = false;
            m_SweepClock.Restart();
            UpdateStatsLookups();
            RebuildScopeSlots();

            CollectResidentHomes();
            CollectScoped(m_CrimeProducerQuery, m_CrimeProducerBuildings);
            CollectScoped(m_FlammableBuildingQuery, m_FlammableBuildings);

            /*
                Every view onto a persistent list is taken before anything is scheduled: asking a list for
                one again once a job has been handed it is what the job safety system is there to catch.
            */
            NativeArray<DistrictStats> totals = m_SweepTotals.AsArray();
            NativeArray<ScopedBuilding> residentHomes = m_ResidentHomes.AsArray();
            NativeArray<DistrictStats> residentResults = m_ResidentResults.AsArray();
            NativeArray<ScopedBuilding> crimeProducers = m_CrimeProducerBuildings.AsArray();
            NativeArray<SumAndCount> crimeTotals = m_CrimeTotals.AsArray();
            NativeArray<Entity> districts = m_ScopeDistricts.AsArray();
            NativeArray<ScopedBuilding> flammableBuildings = m_FlammableBuildings.AsArray();
            NativeArray<SumAndCount> fireRiskTotals = m_FireRiskTotals.AsArray();

            // Every sweep only reads the world and writes into an output of its own, so they all run alongside each other.
            JobHandle crime = new SweepCrimeJob
            {
                m_Buildings = crimeProducers,
                m_CrimeProducers = m_CrimeProducers,
                m_Totals = crimeTotals,
            }.Schedule(Dependency);
            JobHandle fireRisk = new SweepFireHazardJob
            {
                m_Buildings = flammableBuildings,
                m_Districts = districts,
                m_BuildingData = m_Buildings,
                m_BuildingPrefabs = m_BuildingPrefabs,
                m_PrefabFireHazards = m_PrefabFireHazards,
                m_PrefabSpawnableBuildings = m_PrefabSpawnableBuildings,
                m_PrefabZoneProperties = m_PrefabZoneProperties,
                m_BuildingsUnderConstruction = m_BuildingsUnderConstruction,
                m_RoadServiceCoverages = m_RoadServiceCoverages,
                m_DistrictModifiers = m_DistrictModifiers,
                m_Totals = fireRiskTotals,
            }.Schedule(Dependency);

            JobHandle residents = new SweepResidentsJob
            {
                m_Buildings = residentHomes,
                m_Renters = m_Renters,
                m_HouseholdCitizens = m_HouseholdCitizens,
                m_Resources = m_Resources,
                m_Households = m_Households,
                m_Citizens = m_Citizens,
                m_HealthProblems = m_HealthProblems,
                m_TouristHouseholds = m_TouristHouseholds,
                m_CommuterHouseholds = m_CommuterHouseholds,
                m_MovingAwayHouseholds = m_MovingAwayHouseholds,
                m_Results = residentResults,
            }.Schedule(residentHomes.Length, kSweepBatchSize, Dependency);

            // Adding one building's figures into a district's is real work at DistrictStats' size, so the
            // fold runs on a worker thread too; the main thread only ever reads the finished totals.
            m_SweepHandle = new FoldSweepsJob
            {
                m_CrimeTotals = crimeTotals,
                m_FireRiskTotals = fireRiskTotals,
                m_ResidentHomes = residentHomes,
                m_ResidentResults = residentResults,
                m_Totals = totals,
            }.Schedule(JobHandle.CombineDependencies(crime, fireRisk, residents));

            Dependency = m_SweepHandle;
            m_SweepInFlight = true;
            m_SweepScheduleMs = m_SweepClock.Elapsed.TotalMilliseconds;
        }

        // Points the slot map at the districts just collected, and clears the totals they land in.
        private void RebuildScopeSlots()
        {
            m_ScopeSlots.Clear();
            for (int slot = 0; slot < m_ScopeDistricts.Length; slot++)
            {
                m_ScopeSlots.Add(m_ScopeDistricts[slot], slot);
            }
            ClearPerDistrict(m_SweepTotals);
            ClearPerDistrict(m_CrimeTotals);
            ClearPerDistrict(m_FireRiskTotals);
        }

        // Gives one per-district output an empty entry for every district in scope.
        private void ClearPerDistrict<T>(NativeList<T> perDistrict) where T : unmanaged
        {
            perDistrict.Clear();
            perDistrict.Resize(m_ScopeDistricts.Length, NativeArrayOptions.ClearMemory);
        }

        // Narrows one district-keyed query down to the districts in scope.
        private int CollectScoped(EntityQuery query, NativeList<ScopedBuilding> scoped)
        {
            using NativeArray<Entity> buildings = query.ToEntityArray(Allocator.Temp);
            using NativeArray<CurrentDistrict> districts =
                query.ToComponentDataArray<CurrentDistrict>(Allocator.Temp);

            scoped.Clear();

            for (int i = 0; i < buildings.Length; i++)
            {
                if (!m_ScopeSlots.TryGetValue(districts[i].m_District, out int slot))
                {
                    continue;
                }
                scoped.Add(new ScopedBuilding { m_Building = buildings[i], m_Slot = slot });
            }

            return buildings.Length;
        }

        private void CollectResidentHomes()
        {
            m_ResidentHomeCount = CollectScoped(m_ResidentialBuildingQuery, m_ResidentHomes);
            m_ResidentResults.Clear();
            m_ResidentResults.Resize(m_ResidentHomes.Length, NativeArrayOptions.UninitializedMemory);
        }

        // Hands the finished per-district totals to the readers.
        private void PublishSweep()
        {
            double scheduledMs = m_SweepClock.Elapsed.TotalMilliseconds;

            bool finishedUnwatched = m_SweepHandle.IsCompleted;
            m_SweepHandle.Complete();
            m_SweepInFlight = false;
            double blockMs = m_SweepClock.Elapsed.TotalMilliseconds - scheduledMs;

            m_PublishedStats.Clear();
            for (int slot = 0; slot < m_ScopeDistricts.Length; slot++)
            {
                m_PublishedStats[m_ScopeDistricts[slot]] = m_SweepTotals[slot];
            }
            StatsVersion++;

            // The dump below interpolates every total into one string, and that happens whether or not anything is listening,
            // so it is worth asking first.
            if (!Mod.log.isDebugEnabled)
            {
                return;
            }

            double latencyMs = m_SweepClock.Elapsed.TotalMilliseconds;
            double publishMs = latencyMs - scheduledMs - blockMs;

            DistrictStats total = default;
            foreach (DistrictStats districtStats in m_SweepTotals)
            {
                total.Add(districtStats);
            }

            Mod.log.Debug($"Swept district resident stats; main_thread_ms:{m_SweepScheduleMs + blockMs + publishMs:F3} " +
                $"schedule_ms:{m_SweepScheduleMs:F3} block_ms:{blockMs:F3} publish_ms:{publishMs:F3} " +
                $"latency_ms:{latencyMs:F3} finished_early:{finishedUnwatched} building_count:{m_ResidentHomeCount} " +
                $"swept_building_count:{m_ResidentHomes.Length} scope_district_count:{m_ScopeDistricts.Length} " +
                $"district_count:{m_PublishedStats.Count} population:{total.m_Population} household_count:{total.m_HouseholdCount} " +
                $"crime_producer_count:{total.m_CrimeProducerCount} crime_sum:{total.m_CrimeSum:F1} " +
                $"fire_risk_building_count:{total.m_FireRiskBuildingCount} fire_risk_sum:{total.m_FireRiskSum:F1}");
        }

        // Points every lookup the sweeps hop through at the current frame's data.
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

            m_CrimeProducers.Update(this);

            m_Buildings.Update(this);
            m_BuildingPrefabs.Update(this);
            m_PrefabFireHazards.Update(this);
            m_PrefabSpawnableBuildings.Update(this);
            m_PrefabZoneProperties.Update(this);
            m_BuildingsUnderConstruction.Update(this);
            m_RoadServiceCoverages.Update(this);
            m_DistrictModifiers.Update(this);
        }

        // Already-accumulated crime, added straight into each producer's own district.
        private struct SweepCrimeJob : IJob
        {
            [ReadOnly] public NativeArray<ScopedBuilding> m_Buildings;
            [ReadOnly] public ComponentLookup<Game.Buildings.CrimeProducer> m_CrimeProducers;
            public NativeArray<SumAndCount> m_Totals;

            public void Execute()
            {
                foreach (ScopedBuilding scoped in m_Buildings)
                {
                    if (!m_CrimeProducers.TryGetComponent(scoped.m_Building, out Game.Buildings.CrimeProducer producer))
                    {
                        continue;
                    }

                    SumAndCount totals = m_Totals[scoped.m_Slot];
                    totals.m_Sum += producer.m_Crime;
                    totals.m_Count++;
                    m_Totals[scoped.m_Slot] = totals;
                }
            }
        }

        // Vanilla's own per-building fire-hazard formula, reimplemented over this system's own lookups.
        private struct SweepFireHazardJob : IJob
        {
            [ReadOnly] public NativeArray<ScopedBuilding> m_Buildings;
            [ReadOnly] public NativeArray<Entity> m_Districts;
            [ReadOnly] public ComponentLookup<Building> m_BuildingData;
            [ReadOnly] public ComponentLookup<PrefabRef> m_BuildingPrefabs;
            [ReadOnly] public ComponentLookup<DestructibleObjectData> m_PrefabFireHazards;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildings;
            [ReadOnly] public ComponentLookup<ZonePropertiesData> m_PrefabZoneProperties;
            [ReadOnly] public ComponentLookup<Game.Objects.UnderConstruction> m_BuildingsUnderConstruction;
            [ReadOnly] public BufferLookup<Game.Net.ServiceCoverage> m_RoadServiceCoverages;
            [ReadOnly] public BufferLookup<DistrictModifier> m_DistrictModifiers;
            public NativeArray<SumAndCount> m_Totals;

            public void Execute()
            {
                foreach (ScopedBuilding scoped in m_Buildings)
                {
                    if (!TryGetRiskFactor(scoped.m_Building, m_Districts[scoped.m_Slot], out float riskFactor))
                    {
                        continue;
                    }

                    SumAndCount totals = m_Totals[scoped.m_Slot];
                    totals.m_Sum += riskFactor;
                    totals.m_Count++;

                    m_Totals[scoped.m_Slot] = totals;
                }
            }

            private bool TryGetRiskFactor(Entity building, Entity district, out float riskFactor)
            {
                riskFactor = 0f;

                if (!m_BuildingData.TryGetComponent(building, out Building buildingData)
                    || !m_BuildingPrefabs.TryGetComponent(building, out PrefabRef prefabRef))
                {
                    return false;
                }

                float fireHazard = m_PrefabFireHazards.TryGetComponent(prefabRef.m_Prefab, out DestructibleObjectData destructible)
                    ? destructible.m_FireHazard
                    : 100f;

                bool hasUnderConstruction = m_BuildingsUnderConstruction.TryGetComponent(
                    building, out Game.Objects.UnderConstruction underConstruction);
                byte progress = hasUnderConstruction ? underConstruction.m_Progress : byte.MaxValue;
                Entity newPrefab = hasUnderConstruction ? underConstruction.m_NewPrefab : Entity.Null;

                if (newPrefab == Entity.Null && progress < byte.MaxValue)
                {
                    fireHazard = 0f;
                }

                if (fireHazard == 0f)
                {
                    return false;
                }

                if (m_PrefabSpawnableBuildings.TryGetComponent(prefabRef.m_Prefab, out SpawnableBuildingData spawnable))
                {
                    fireHazard *= 1f - (spawnable.m_Level - 1) * 0.03f;

                    if (m_PrefabZoneProperties.TryGetComponent(spawnable.m_ZonePrefab, out ZonePropertiesData zone))
                    {
                        fireHazard *= zone.m_FireHazardMultiplier;
                    }
                }

                float coverage = 0f;
                if (m_RoadServiceCoverages.TryGetBuffer(buildingData.m_RoadEdge, out DynamicBuffer<Game.Net.ServiceCoverage> coverages))
                {
                    coverage = Game.Net.NetUtils.GetServiceCoverage(coverages, Game.Net.CoverageService.FireRescue, buildingData.m_CurvePosition);
                    fireHazard *= math.max(0.01f, 1f - coverage * 0.01f);
                }

                if (m_DistrictModifiers.TryGetBuffer(district, out DynamicBuffer<DistrictModifier> modifiers))
                {
                    AreaUtils.ApplyModifier(ref fireHazard, modifiers, DistrictModifierType.BuildingFireHazard);
                }

                riskFactor = fireHazard / (1f + coverage * 0.5f);

                return true;
            }
        }

        // Adds what every sweep found into one set of totals per district in scope.
        private struct FoldSweepsJob : IJob
        {
            [ReadOnly] public NativeArray<SumAndCount> m_CrimeTotals;
            [ReadOnly] public NativeArray<SumAndCount> m_FireRiskTotals;
            [ReadOnly] public NativeArray<ScopedBuilding> m_ResidentHomes;
            [ReadOnly] public NativeArray<DistrictStats> m_ResidentResults;
            public NativeArray<DistrictStats> m_Totals;

            public void Execute()
            {
                // The district-keyed sweeps each own their own fields, so their figures are taken rather than added to.
                for (int slot = 0; slot < m_Totals.Length; slot++)
                {
                    DistrictStats totals = m_Totals[slot];
                    totals.m_CrimeSum = m_CrimeTotals[slot].m_Sum;
                    totals.m_CrimeProducerCount = m_CrimeTotals[slot].m_Count;
                    totals.m_FireRiskSum = m_FireRiskTotals[slot].m_Sum;
                    totals.m_FireRiskBuildingCount = m_FireRiskTotals[slot].m_Count;
                    m_Totals[slot] = totals;
                }

                for (int i = 0; i < m_ResidentHomes.Length; i++)
                {
                    int slot = m_ResidentHomes[i].m_Slot;
                    DistrictStats totals = m_Totals[slot];
                    totals.Add(m_ResidentResults[i]);
                    m_Totals[slot] = totals;
                }
            }
        }

        // One in-scope building's residents per iteration, summed into that building's own result slot.
        private struct SweepResidentsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ScopedBuilding> m_Buildings;
            [ReadOnly] public BufferLookup<Renter> m_Renters;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly] public BufferLookup<Resources> m_Resources;
            [ReadOnly] public ComponentLookup<Household> m_Households;
            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HealthProblem> m_HealthProblems;
            [ReadOnly] public ComponentLookup<TouristHousehold> m_TouristHouseholds;
            [ReadOnly] public ComponentLookup<CommuterHousehold> m_CommuterHouseholds;
            [ReadOnly] public ComponentLookup<MovingAway> m_MovingAwayHouseholds;
            [WriteOnly] public NativeArray<DistrictStats> m_Results;

            public void Execute(int index)
            {
                DistrictStats stats = default;
                AccumulateBuilding(m_Buildings[index].m_Building, ref stats);
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
                    and income averages leave out tourists, commuters and households already on their way out
                    of the city.
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
                stats.m_IncomeSum += householdData.m_Income;
                stats.m_HouseholdCount++;
            }
        }
    }
}

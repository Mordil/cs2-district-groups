using Game;
using Game.Agents;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Economy;
using Game.Prefabs;
using Game.Simulation;
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
        // How old a child is before vanilla counts them as needing an elementary place, in days.
        private const int kElementaryEligibleAgeDays = 10;
        // How many rejections vanilla lets a citizen collect before it stops offering them higher education.
        private const int kMaxFailedEducationAttempts = 3;

        // Residential buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_ResidentialBuildingQuery;
        // Crime-producing buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_CrimeProducerQuery;
        // Flammable buildings, keyed to a district via CurrentDistrict; excludes fire stations and buildings already
        // on fire, which contribute no fire risk of their own.
        private EntityQuery m_FlammableBuildingQuery;
        // Hospitals with at least one occupied patient slot; every patient is hopped back to their own home district
        // rather than the hospital's, which may be anywhere in the city.
        private EntityQuery m_HospitalQuery;
        // Garbage-producing buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_GarbageProducerQuery;
        // Holds the balances a building's garbage accumulation is weighed with.
        private EntityQuery m_GarbageParameterQuery;
        // Mail-producing buildings, keyed to a district via CurrentDistrict.
        private EntityQuery m_MailProducerQuery;
        // Holds the wealth thresholds the average household wealth is bucketed against.
        private EntityQuery m_CitizenHappinessParameterQuery;
        // Holds the crime accumulation ceiling a district's average crime is read as a share of.
        private EntityQuery m_PoliceConfigurationQuery;
        // Holds the death-rate curve a resident's chance of dying of old age is read off.
        private EntityQuery m_HealthcareParameterQuery;
        // Holds the probabilities a citizen's chance of entering a school level is weighed with.
        private EntityQuery m_EducationParameterQuery;
        // Holds the year length a resident's age is turned into a share of a full lifetime with.
        private EntityQuery m_TimeSettingsQuery;
        // Holds the calendar a resident's age is read off.
        private EntityQuery m_TimeDataQuery;

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
        private ComponentLookup<Game.Citizens.Student> m_Students;
        private ComponentLookup<Worker> m_Workers;
        private ComponentLookup<HasJobSeeker> m_JobSeekers;
        // The hops from a hospital's patient back to the district their home is in: citizen -> household,
        // household -> the building it rents or shelters in, building -> district.
        private BufferLookup<Patient> m_Patients;
        private ComponentLookup<HouseholdMember> m_HouseholdMembers;
        private ComponentLookup<PropertyRenter> m_PropertyRenters;
        private ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
        private ComponentLookup<CurrentDistrict> m_BuildingDistricts;
        // What a district's own membership is tested with while the scope is being collected.
        private ComponentLookup<District> m_DistrictAreas;
        private ComponentLookup<Game.Common.Deleted> m_DeletedEntities;
        private BufferLookup<DistrictGroupMember> m_GroupMembers;

        private ComponentLookup<Game.Buildings.CrimeProducer> m_CrimeProducers;
        private ComponentLookup<Game.Buildings.MailProducer> m_MailProducers;

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

        // The rest of what vanilla's public GarbageAccumulationSystem.GetGarbageAccumulation asks for, which the garbage
        // sweep calls rather than restating a formula weighing occupancy, education, level, homelessness and modifiers.
        private ComponentLookup<ConsumptionData> m_PrefabConsumptions;
        private ComponentLookup<ZoneData> m_PrefabZoneDatas;
        private BufferLookup<InstalledUpgrade> m_InstalledUpgrades;
        private BufferLookup<Game.Companies.Employee> m_Employees;
        private BufferLookup<Game.Buildings.Student> m_BuildingStudents;
        private BufferLookup<Occupant> m_Occupants;
        private BufferLookup<CityModifier> m_CityModifiers;

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
        private NativeList<ScopedBuilding> m_GarbageProducers;
        private NativeList<float> m_GarbageResults;
        private NativeList<ScopedBuilding> m_CrimeProducerBuildings;
        private NativeList<ScopedBuilding> m_FlammableBuildings;
        private NativeList<ScopedBuilding> m_MailProducerBuildings;
        // Every hospital in the city, unnarrowed: a patient is credited to their own home district, which is not
        // necessarily the one the hospital sits in.
        private NativeList<Entity> m_Hospitals;

        private NativeList<SumAndCount> m_CrimeTotals;
        private NativeList<SumAndCount> m_FireRiskTotals;
        private NativeList<int> m_PatientTotals;
        private NativeList<int> m_MailTotals;

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
        private SimulationSystem m_SimulationSystem;
        private DeathCheckSystem m_DeathCheckSystem;
        private TimeSystem m_TimeSystem;
        private CitySystem m_CitySystem;

        /*
            HealthcareParameterData carries two death-rate curves, and a city started before the newer one was
            introduced keeps reading the legacy one forever. Which one is live is a private field on DeathCheckSystem
            with no accessor, so it's read by reflection; a build where it's been renamed away falls back to the newer
            curve, which every city started since uses.
        */
        private System.Reflection.FieldInfo m_UseNewDeathRateField;

        private int m_ResidentHomeCount;
        private bool m_SweepDeathcareReady;
        private bool m_SweepGarbageReady;
        private bool m_SweepEducationReady;
        private bool m_SweepHasCityModifiers;
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
            m_SimulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            m_DeathCheckSystem = World.GetOrCreateSystemManaged<DeathCheckSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
            m_UseNewDeathRateField = typeof(DeathCheckSystem).GetField(
                "m_UseNewCurve",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Mod.log.Info($"Resolved death rate curve selector; found:{m_UseNewDeathRateField != null}");

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
            m_HospitalQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Buildings.Hospital>(),
                ComponentType.ReadOnly<Patient>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            // Vanilla also excludes Destroyed: DestroySystem strips GarbageProducer off rubble in its own pass, so a
            // building destroyed this frame can still match.
            m_GarbageProducerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Buildings.GarbageProducer>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.Exclude<Game.Common.Destroyed>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_GarbageParameterQuery = GetEntityQuery(ComponentType.ReadOnly<GarbageParameterData>());
            m_MailProducerQuery = GetEntityQuery(
                ComponentType.ReadOnly<Game.Buildings.MailProducer>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Game.Tools.Temp>(),
                ComponentType.Exclude<Game.Common.Deleted>());
            m_CitizenHappinessParameterQuery = GetEntityQuery(ComponentType.ReadOnly<CitizenHappinessParameterData>());
            m_PoliceConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<PoliceConfigurationData>());
            m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
            m_EducationParameterQuery = GetEntityQuery(ComponentType.ReadOnly<EducationParameterData>());
            m_TimeSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<TimeSettingsData>());
            m_TimeDataQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Common.TimeData>());

            m_Renters = GetBufferLookup<Renter>(true);
            m_HouseholdCitizens = GetBufferLookup<HouseholdCitizen>(true);
            m_Resources = GetBufferLookup<Resources>(true);
            m_Households = GetComponentLookup<Household>(true);
            m_Citizens = GetComponentLookup<Citizen>(true);
            m_HealthProblems = GetComponentLookup<HealthProblem>(true);
            m_TouristHouseholds = GetComponentLookup<TouristHousehold>(true);
            m_CommuterHouseholds = GetComponentLookup<CommuterHousehold>(true);
            m_MovingAwayHouseholds = GetComponentLookup<MovingAway>(true);
            m_Students = GetComponentLookup<Game.Citizens.Student>(true);
            m_Workers = GetComponentLookup<Worker>(true);
            m_JobSeekers = GetComponentLookup<HasJobSeeker>(true);
            m_Patients = GetBufferLookup<Patient>(true);
            m_HouseholdMembers = GetComponentLookup<HouseholdMember>(true);
            m_PropertyRenters = GetComponentLookup<PropertyRenter>(true);
            m_HomelessHouseholds = GetComponentLookup<HomelessHousehold>(true);
            m_BuildingDistricts = GetComponentLookup<CurrentDistrict>(true);
            m_DistrictAreas = GetComponentLookup<District>(true);
            m_DeletedEntities = GetComponentLookup<Game.Common.Deleted>(true);
            m_GroupMembers = GetBufferLookup<DistrictGroupMember>(true);

            m_CrimeProducers = GetComponentLookup<Game.Buildings.CrimeProducer>(true);
            m_MailProducers = GetComponentLookup<Game.Buildings.MailProducer>(true);

            m_Buildings = GetComponentLookup<Building>(true);
            m_BuildingPrefabs = GetComponentLookup<PrefabRef>(true);
            m_PrefabFireHazards = GetComponentLookup<DestructibleObjectData>(true);
            m_PrefabSpawnableBuildings = GetComponentLookup<SpawnableBuildingData>(true);
            m_PrefabZoneProperties = GetComponentLookup<ZonePropertiesData>(true);
            m_BuildingsUnderConstruction = GetComponentLookup<Game.Objects.UnderConstruction>(true);
            m_RoadServiceCoverages = GetBufferLookup<Game.Net.ServiceCoverage>(true);
            m_DistrictModifiers = GetBufferLookup<DistrictModifier>(true);

            m_PrefabConsumptions = GetComponentLookup<ConsumptionData>(true);
            m_PrefabZoneDatas = GetComponentLookup<ZoneData>(true);
            m_InstalledUpgrades = GetBufferLookup<InstalledUpgrade>(true);
            m_Employees = GetBufferLookup<Game.Companies.Employee>(true);
            m_BuildingStudents = GetBufferLookup<Game.Buildings.Student>(true);
            m_Occupants = GetBufferLookup<Occupant>(true);
            m_CityModifiers = GetBufferLookup<CityModifier>(true);

            m_ScopeDistricts = new NativeList<Entity>(Allocator.Persistent);
            m_ScopeSlots = new NativeHashMap<Entity, int>(16, Allocator.Persistent);
            m_ScopeSeen = new NativeHashSet<Entity>(16, Allocator.Persistent);
            m_SweepTotals = new NativeList<DistrictStats>(Allocator.Persistent);

            m_ResidentHomes = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_ResidentResults = new NativeList<DistrictStats>(Allocator.Persistent);
            m_GarbageProducers = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_GarbageResults = new NativeList<float>(Allocator.Persistent);
            m_CrimeProducerBuildings = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_FlammableBuildings = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_MailProducerBuildings = new NativeList<ScopedBuilding>(Allocator.Persistent);
            m_Hospitals = new NativeList<Entity>(Allocator.Persistent);
            m_CrimeTotals = new NativeList<SumAndCount>(Allocator.Persistent);
            m_FireRiskTotals = new NativeList<SumAndCount>(Allocator.Persistent);
            m_PatientTotals = new NativeList<int>(Allocator.Persistent);
            m_MailTotals = new NativeList<int>(Allocator.Persistent);
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
            m_GarbageProducers.Dispose();
            m_GarbageResults.Dispose();
            m_CrimeProducerBuildings.Dispose();
            m_FlammableBuildings.Dispose();
            m_MailProducerBuildings.Dispose();
            m_Hospitals.Dispose();
            m_CrimeTotals.Dispose();
            m_FireRiskTotals.Dispose();
            m_PatientTotals.Dispose();
            m_MailTotals.Dispose();
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
            CollectScoped(m_MailProducerQuery, m_MailProducerBuildings);
            CollectHospitals();

            GarbageContext garbage = GetGarbageContext();
            m_SweepGarbageReady = garbage.m_Valid;
            CollectGarbageProducers(garbage.m_Valid);

            EducationContext education = GetEducationContext();
            m_SweepEducationReady = education.m_Valid;
            m_SweepHasCityModifiers = education.m_Valid && EntityManager.HasBuffer<CityModifier>(education.m_City);

            DeathcareContext deathcare = GetDeathcareContext();
            m_SweepDeathcareReady = deathcare.m_Valid;

            /*
                Every view onto a persistent list is taken before anything is scheduled: asking a list for
                one again once a job has been handed it is what the job safety system is there to catch.
            */
            NativeArray<DistrictStats> totals = m_SweepTotals.AsArray();
            NativeArray<ScopedBuilding> residentHomes = m_ResidentHomes.AsArray();
            NativeArray<DistrictStats> residentResults = m_ResidentResults.AsArray();
            NativeArray<ScopedBuilding> garbageProducers = m_GarbageProducers.AsArray();
            NativeArray<float> garbageResults = m_GarbageResults.AsArray();
            NativeArray<ScopedBuilding> crimeProducers = m_CrimeProducerBuildings.AsArray();
            NativeArray<SumAndCount> crimeTotals = m_CrimeTotals.AsArray();
            NativeArray<Entity> districts = m_ScopeDistricts.AsArray();
            NativeArray<ScopedBuilding> flammableBuildings = m_FlammableBuildings.AsArray();
            NativeArray<SumAndCount> fireRiskTotals = m_FireRiskTotals.AsArray();
            NativeArray<Entity> hospitals = m_Hospitals.AsArray();
            NativeArray<int> patientTotals = m_PatientTotals.AsArray();
            NativeArray<ScopedBuilding> mailProducers = m_MailProducerBuildings.AsArray();
            NativeArray<int> mailTotals = m_MailTotals.AsArray();

            // Every sweep only reads the world and writes into an output of its own, so they all run alongside each other.
            JobHandle crime = new SweepCrimeJob
            {
                m_Buildings = crimeProducers,
                m_CrimeProducers = m_CrimeProducers,
                m_Totals = crimeTotals,
            }.Schedule(Dependency);
            JobHandle mail = new SweepMailJob
            {
                m_Buildings = mailProducers,
                m_MailProducers = m_MailProducers,
                m_Totals = mailTotals,
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
            JobHandle patients = new SweepActivePatientsJob
            {
                m_Hospitals = hospitals,
                m_Patients = m_Patients,
                m_HouseholdMembers = m_HouseholdMembers,
                m_PropertyRenters = m_PropertyRenters,
                m_HomelessHouseholds = m_HomelessHouseholds,
                m_BuildingDistricts = m_BuildingDistricts,
                m_Slots = m_ScopeSlots,
                m_Totals = patientTotals,
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
                m_Students = m_Students,
                m_Workers = m_Workers,
                m_JobSeekers = m_JobSeekers,
                m_CityModifiers = m_CityModifiers,
                m_Education = education,
                m_Deathcare = deathcare,
                m_Results = residentResults,
            }.Schedule(residentHomes.Length, kSweepBatchSize, Dependency);
            JobHandle garbageRates = new SweepGarbageJob
            {
                m_Buildings = garbageProducers,
                m_Districts = districts,
                m_BuildingPrefabs = m_BuildingPrefabs,
                m_PrefabConsumptions = m_PrefabConsumptions,
                m_PrefabSpawnableBuildings = m_PrefabSpawnableBuildings,
                m_PrefabZoneDatas = m_PrefabZoneDatas,
                m_Citizens = m_Citizens,
                m_HomelessHouseholds = m_HomelessHouseholds,
                m_InstalledUpgrades = m_InstalledUpgrades,
                m_Renters = m_Renters,
                m_HouseholdCitizens = m_HouseholdCitizens,
                m_Employees = m_Employees,
                m_BuildingStudents = m_BuildingStudents,
                m_Occupants = m_Occupants,
                m_Patients = m_Patients,
                m_DistrictModifiers = m_DistrictModifiers,
                m_CityModifiers = m_CityModifiers,
                m_Garbage = garbage,
                m_Results = garbageResults,
            }.Schedule(garbageProducers.Length, kSweepBatchSize, Dependency);

            // Adding one building's figures into a district's is real work at DistrictStats' size, so the
            // fold runs on a worker thread too; the main thread only ever reads the finished totals.
            m_SweepHandle = new FoldSweepsJob
            {
                m_CrimeTotals = crimeTotals,
                m_FireRiskTotals = fireRiskTotals,
                m_PatientTotals = patientTotals,
                m_MailTotals = mailTotals,
                m_ResidentHomes = residentHomes,
                m_ResidentResults = residentResults,
                m_GarbageProducers = garbageProducers,
                m_GarbageResults = garbageResults,
                m_Totals = totals,
            }.Schedule(JobHandle.CombineDependencies(
                JobHandle.CombineDependencies(crime, mail, fireRisk),
                JobHandle.CombineDependencies(patients, residents, garbageRates)));

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
            ClearPerDistrict(m_PatientTotals);
            ClearPerDistrict(m_MailTotals);
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

        // Narrows the garbage producers down to the districts in scope, or collects none at all when the city hasn't
        // loaded its garbage parameters, so the panel reads nothing rather than a confident zero.
        private void CollectGarbageProducers(bool ready)
        {
            m_GarbageResults.Clear();
            if (!ready)
            {
                m_GarbageProducers.Clear();
                return;
            }

            CollectScoped(m_GarbageProducerQuery, m_GarbageProducers);
            m_GarbageResults.Resize(m_GarbageProducers.Length, NativeArrayOptions.UninitializedMemory);
        }

        // Every hospital in the city, since a patient is credited to their own home district rather than to whichever
        // district the hospital itself sits in.
        private void CollectHospitals()
        {
            using NativeArray<Entity> hospitals = m_HospitalQuery.ToEntityArray(Allocator.Temp);
            m_Hospitals.Clear();
            m_Hospitals.AddRange(hospitals);
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
                $"fire_risk_building_count:{total.m_FireRiskBuildingCount} fire_risk_sum:{total.m_FireRiskSum:F1} " +
                $"settled_resident_count:{total.m_SettledResidentCount} health_sum:{total.m_HealthSum} " +
                $"active_patient_count:{total.m_ActivePatientCount} mail_generation_sum:{total.m_MailGenerationSum} " +
                $"deathcare_ready:{m_SweepDeathcareReady} " +
                $"education_ready:{m_SweepEducationReady} city_modifiers:{m_SweepHasCityModifiers} " +
                $"eligible_elementary:{total.m_EligibleSums.x:F1} eligible_high_school:{total.m_EligibleSums.y:F1} " +
                $"eligible_college:{total.m_EligibleSums.z:F1} eligible_university:{total.m_EligibleSums.w:F1} " +
                $"enrolled_elementary:{total.m_EnrolledCounts.x} enrolled_high_school:{total.m_EnrolledCounts.y} " +
                $"enrolled_college:{total.m_EnrolledCounts.z} enrolled_university:{total.m_EnrolledCounts.w} " +
                $"garbage_ready:{m_SweepGarbageReady} swept_garbage_producer_count:{m_GarbageProducers.Length} " +
                $"garbage_producer_count:{total.m_GarbageProducerCount} " +
                $"garbage_accumulation_sum:{total.m_GarbageAccumulationSum:F1} " +
                $"death_rate_resident_count:{total.m_DeathRateResidentCount} death_rate_sum:{total.m_DeathRateSum:F2}");
        }

        // The city-wide inputs a building's garbage accumulation is weighed against.
        private GarbageContext GetGarbageContext()
        {
            if (m_GarbageParameterQuery.IsEmptyIgnoreFilter || m_CitySystem.City == Entity.Null)
            {
                return default;
            }

            return new GarbageContext
            {
                m_Valid = true,
                m_City = m_CitySystem.City,
                m_Parameters = m_GarbageParameterQuery.GetSingleton<GarbageParameterData>(),
            };
        }

        // The city-wide inputs vanilla weighs a citizen's chance of entering a school level against.
        private EducationContext GetEducationContext()
        {
            if (m_EducationParameterQuery.IsEmptyIgnoreFilter
                || m_TimeDataQuery.IsEmptyIgnoreFilter
                || m_CitySystem.City == Entity.Null)
            {
                return default;
            }

            return new EducationContext
            {
                m_Valid = true,
                m_City = m_CitySystem.City,
                m_Parameters = m_EducationParameterQuery.GetSingleton<EducationParameterData>(),
                m_TimeData = m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                m_SimulationFrame = m_SimulationSystem.frameIndex,
            };
        }

        // The city-wide inputs the game weighs a resident's chance of dying against.
        private DeathcareContext GetDeathcareContext()
        {
            if (m_HealthcareParameterQuery.IsEmptyIgnoreFilter
                || m_TimeSettingsQuery.IsEmptyIgnoreFilter
                || m_TimeDataQuery.IsEmptyIgnoreFilter)
            {
                return default;
            }

            TimeSettingsData timeSettings = m_TimeSettingsQuery.GetSingleton<TimeSettingsData>();
            if (timeSettings.m_DaysPerYear <= 0)
            {
                return default;
            }

            return new DeathcareContext
            {
                m_Valid = true,
                m_Parameters = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
                m_UseNewCurve = UsesNewDeathRateCurve(),
                m_DaysPerYear = timeSettings.m_DaysPerYear,
                m_TimeData = m_TimeDataQuery.GetSingleton<Game.Common.TimeData>(),
                m_SimulationFrame = m_SimulationSystem.frameIndex,
                m_NormalizedTime = m_TimeSystem.normalizedTime,
            };
        }

        // Which of the two death-rate curves this save is being run against - see m_UseNewDeathRateField.
        private bool UsesNewDeathRateCurve()
        {
            if (m_UseNewDeathRateField == null || m_DeathCheckSystem == null)
            {
                return true;
            }

            return m_UseNewDeathRateField.GetValue(m_DeathCheckSystem) is bool useNewCurve && useNewCurve;
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
            m_Students.Update(this);
            m_Workers.Update(this);
            m_JobSeekers.Update(this);
            m_Patients.Update(this);
            m_HouseholdMembers.Update(this);
            m_PropertyRenters.Update(this);
            m_HomelessHouseholds.Update(this);
            m_BuildingDistricts.Update(this);

            m_CrimeProducers.Update(this);
            m_MailProducers.Update(this);

            m_Buildings.Update(this);
            m_BuildingPrefabs.Update(this);
            m_PrefabFireHazards.Update(this);
            m_PrefabSpawnableBuildings.Update(this);
            m_PrefabZoneProperties.Update(this);
            m_BuildingsUnderConstruction.Update(this);
            m_RoadServiceCoverages.Update(this);
            m_DistrictModifiers.Update(this);

            m_PrefabConsumptions.Update(this);
            m_PrefabZoneDatas.Update(this);
            m_InstalledUpgrades.Update(this);
            m_Employees.Update(this);
            m_BuildingStudents.Update(this);
            m_Occupants.Update(this);
            m_CityModifiers.Update(this);
        }

        // Whatever is the same for every building the garbage sweep visits.
        private struct GarbageContext
        {
            // Whether the city had these loaded when the sweep was scheduled.
            public bool m_Valid;
            public Entity m_City;
            public GarbageParameterData m_Parameters;
        }

        // Whatever is the same for every citizen the eligibility half of the resident sweep weighs.
        private struct EducationContext
        {
            // Whether the city had these loaded when the sweep was scheduled.
            public bool m_Valid;
            public Entity m_City;
            public EducationParameterData m_Parameters;
            public Game.Common.TimeData m_TimeData;
            public uint m_SimulationFrame;
        }

        // The city-wide inputs the game weighs a citizen's chance of dying against.
        private struct DeathcareContext
        {
            // Whether the city had these loaded when the sweep was scheduled.
            public bool m_Valid;
            public HealthcareParameterData m_Parameters;
            // Which of the two curves in the parameters this save is being run against.
            public bool m_UseNewCurve;
            public int m_DaysPerYear;
            public Game.Common.TimeData m_TimeData;
            public uint m_SimulationFrame;
            // Where in the day the sweep is reading from, which is where the game centres a citizen's age.
            public float m_NormalizedTime;
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

        // Every mail-producing building's backlog, credited to its own district - mail still waiting to be sent or
        // received is exactly what competes for the group's post facilities' storage.
        private struct SweepMailJob : IJob
        {
            [ReadOnly] public NativeArray<ScopedBuilding> m_Buildings;
            [ReadOnly] public ComponentLookup<Game.Buildings.MailProducer> m_MailProducers;
            public NativeArray<int> m_Totals;

            public void Execute()
            {
                foreach (ScopedBuilding scoped in m_Buildings)
                {
                    if (!m_MailProducers.TryGetComponent(scoped.m_Building, out Game.Buildings.MailProducer producer))
                    {
                        continue;
                    }

                    m_Totals[scoped.m_Slot] += producer.m_SendingMail + producer.receivingMail;
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

        /*
            Every occupied hospital patient slot in the city, credited back to the patient's own home district. Unlike
            the other sweeps this one can't be narrowed to a district beforehand: which district a patient belongs to
            only comes out of hopping from them to their household, to the building it lives in, and to its district.
        */
        private struct SweepActivePatientsJob : IJob
        {
            [ReadOnly] public NativeArray<Entity> m_Hospitals;
            [ReadOnly] public BufferLookup<Patient> m_Patients;
            [ReadOnly] public ComponentLookup<HouseholdMember> m_HouseholdMembers;
            [ReadOnly] public ComponentLookup<PropertyRenter> m_PropertyRenters;
            [ReadOnly] public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
            [ReadOnly] public ComponentLookup<CurrentDistrict> m_BuildingDistricts;
            [ReadOnly] public NativeHashMap<Entity, int> m_Slots;
            public NativeArray<int> m_Totals;

            public void Execute()
            {
                foreach (Entity hospital in m_Hospitals)
                {
                    if (!m_Patients.TryGetBuffer(hospital, out DynamicBuffer<Patient> patients))
                    {
                        continue;
                    }

                    foreach (Patient patient in patients)
                    {
                        if (TryGetHomeSlot(patient.m_Patient, out int slot))
                        {
                            m_Totals[slot]++;
                        }
                    }
                }
            }

            // Hops from a citizen to the slot of the district the building they live in sits in, or false for a
            // citizen with no in-scope home to attribute the patient to.
            private bool TryGetHomeSlot(Entity citizen, out int slot)
            {
                slot = 0;
                if (!m_HouseholdMembers.TryGetComponent(citizen, out HouseholdMember member))
                {
                    return false;
                }

                if (!m_BuildingDistricts.TryGetComponent(GetHomeBuilding(member.m_Household), out CurrentDistrict current))
                {
                    return false;
                }

                return m_Slots.TryGetValue(current.m_District, out slot);
            }

            /*
                The building a household lives in, whether it rents the place or only shelters there. Vanilla keeps
                the two on separate components and reads them in this order in BuildingUtils.GetHouseholdHomeBuilding,
                so a household that has both is credited to the property it actually rents.
            */
            private Entity GetHomeBuilding(Entity household)
            {
                if (m_PropertyRenters.TryGetComponent(household, out PropertyRenter renter))
                {
                    return renter.m_Property;
                }

                if (m_HomelessHouseholds.TryGetComponent(household, out HomelessHousehold homeless))
                {
                    return homeless.m_TempHome;
                }

                return Entity.Null;
            }
        }

        // Adds what every sweep found into one set of totals per district in scope.
        private struct FoldSweepsJob : IJob
        {
            [ReadOnly] public NativeArray<SumAndCount> m_CrimeTotals;
            [ReadOnly] public NativeArray<SumAndCount> m_FireRiskTotals;
            [ReadOnly] public NativeArray<int> m_PatientTotals;
            [ReadOnly] public NativeArray<int> m_MailTotals;
            [ReadOnly] public NativeArray<ScopedBuilding> m_ResidentHomes;
            [ReadOnly] public NativeArray<DistrictStats> m_ResidentResults;
            [ReadOnly] public NativeArray<ScopedBuilding> m_GarbageProducers;
            [ReadOnly] public NativeArray<float> m_GarbageResults;
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
                    totals.m_ActivePatientCount = m_PatientTotals[slot];
                    totals.m_MailGenerationSum = m_MailTotals[slot];
                    m_Totals[slot] = totals;
                }

                for (int i = 0; i < m_ResidentHomes.Length; i++)
                {
                    int slot = m_ResidentHomes[i].m_Slot;
                    DistrictStats totals = m_Totals[slot];
                    totals.Add(m_ResidentResults[i]);
                    m_Totals[slot] = totals;
                }

                for (int i = 0; i < m_GarbageProducers.Length; i++)
                {
                    int slot = m_GarbageProducers[i].m_Slot;
                    DistrictStats totals = m_Totals[slot];
                    totals.m_GarbageAccumulationSum += m_GarbageResults[i];
                    totals.m_GarbageProducerCount++;
                    m_Totals[slot] = totals;
                }
            }
        }

        /*
            One in-scope garbage producer per iteration, its own daily rate into its own result slot.

            Nothing stores the rate: GarbageAccumulationSystem pulls ConsumptionData off the prefab into a local, adjusts it
            and discards it. The adjustment walks the building's citizens, which is what makes this a job.
        */
        private struct SweepGarbageJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<ScopedBuilding> m_Buildings;
            [ReadOnly] public NativeArray<Entity> m_Districts;
            [ReadOnly] public ComponentLookup<PrefabRef> m_BuildingPrefabs;
            [ReadOnly] public ComponentLookup<ConsumptionData> m_PrefabConsumptions;
            [ReadOnly] public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildings;
            [ReadOnly] public ComponentLookup<ZoneData> m_PrefabZoneDatas;
            [ReadOnly] public ComponentLookup<Citizen> m_Citizens;
            [ReadOnly] public ComponentLookup<HomelessHousehold> m_HomelessHouseholds;
            [ReadOnly] public BufferLookup<InstalledUpgrade> m_InstalledUpgrades;
            [ReadOnly] public BufferLookup<Renter> m_Renters;
            [ReadOnly] public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;
            [ReadOnly] public BufferLookup<Game.Companies.Employee> m_Employees;
            [ReadOnly] public BufferLookup<Game.Buildings.Student> m_BuildingStudents;
            [ReadOnly] public BufferLookup<Occupant> m_Occupants;
            [ReadOnly] public BufferLookup<Patient> m_Patients;
            [ReadOnly] public BufferLookup<DistrictModifier> m_DistrictModifiers;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifiers;
            public GarbageContext m_Garbage;
            [WriteOnly] public NativeArray<float> m_Results;

            public void Execute(int index)
            {
                m_Results[index] = 0f;
                if (!m_CityModifiers.TryGetBuffer(m_Garbage.m_City, out DynamicBuffer<CityModifier> cityModifiers))
                {
                    return;
                }

                Entity producer = m_Buildings[index].m_Building;
                if (!m_BuildingPrefabs.TryGetComponent(producer, out PrefabRef prefabRef))
                {
                    return;
                }

                Entity prefab = prefabRef.m_Prefab;
                m_PrefabConsumptions.TryGetComponent(prefab, out ConsumptionData consumption);

                // Upgrades change what a building produces, the same as they change a facility's throughput.
                if (m_InstalledUpgrades.TryGetBuffer(producer, out DynamicBuffer<InstalledUpgrade> upgrades)
                    && upgrades.Length != 0)
                {
                    UpgradeUtils.CombineStats(ref consumption, upgrades, ref m_BuildingPrefabs, ref m_PrefabConsumptions);
                }

                GarbageParameterData parameters = m_Garbage.m_Parameters;
                GarbageAccumulationSystem.GetGarbageAccumulation(
                    producer,
                    prefab,
                    ref consumption,
                    new CurrentDistrict { m_District = m_Districts[m_Buildings[index].m_Slot] },
                    cityModifiers,
                    m_Citizens,
                    m_PrefabSpawnableBuildings,
                    m_PrefabZoneDatas,
                    m_HomelessHouseholds,
                    m_HouseholdCitizens,
                    m_Renters,
                    m_Employees,
                    m_BuildingStudents,
                    m_Occupants,
                    m_Patients,
                    m_DistrictModifiers,
                    ref parameters);

                m_Results[index] = consumption.m_GarbageAccumulation;
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
            [ReadOnly] public ComponentLookup<Game.Citizens.Student> m_Students;
            [ReadOnly] public ComponentLookup<Worker> m_Workers;
            [ReadOnly] public ComponentLookup<HasJobSeeker> m_JobSeekers;
            [ReadOnly] public BufferLookup<CityModifier> m_CityModifiers;
            public EducationContext m_Education;
            public DeathcareContext m_Deathcare;
            [WriteOnly] public NativeArray<DistrictStats> m_Results;

            public void Execute(int index)
            {
                // The city, and so its modifiers, is the same for every citizen, so the lookup is done once per building.
                DynamicBuffer<CityModifier> cityModifiers = default;
                bool hasCityModifiers = m_Education.m_Valid
                    && m_CityModifiers.TryGetBuffer(m_Education.m_City, out cityModifiers);

                DistrictStats stats = default;
                AccumulateBuilding(m_Buildings[index].m_Building, hasCityModifiers, cityModifiers, ref stats);
                m_Results[index] = stats;
            }

            // Adds one residential building's renter households to the totals.
            private void AccumulateBuilding(
                Entity building,
                bool hasCityModifiers,
                DynamicBuffer<CityModifier> cityModifiers,
                ref DistrictStats stats)
            {
                if (!m_Renters.TryGetBuffer(building, out DynamicBuffer<Renter> renters))
                {
                    return;
                }
                foreach (Renter renter in renters)
                {
                    AccumulateHousehold(renter.m_Renter, hasCityModifiers, cityModifiers, ref stats);
                }
            }

            private void AccumulateHousehold(
                Entity household,
                bool hasCityModifiers,
                DynamicBuffer<CityModifier> cityModifiers,
                ref DistrictStats stats)
            {
                /*
                    Vanilla's sweeps disagree on which households they take, so each accumulator keeps its own
                    filter: happiness counts every renter household's living citizens, the wealth and income
                    averages leave out tourists, commuters and households already on their way out of the city,
                    and the health and education totals additionally leave out a household that has not finished
                    moving in - it has nobody in the city to treat or school yet.
                */

                bool isHousehold = m_Households.TryGetComponent(household, out Household householdData);
                bool hasResidents = m_HouseholdCitizens.TryGetBuffer(household, out DynamicBuffer<HouseholdCitizen> residents);
                if (!isHousehold || !hasResidents)
                {
                    return;
                }

                bool isTemporaryResident = m_TouristHouseholds.HasComponent(household)
                    || m_CommuterHouseholds.HasComponent(household)
                    || m_MovingAwayHouseholds.HasComponent(household);
                bool hasMovedIn = (householdData.m_Flags & HouseholdFlags.MovedIn) != 0;
                bool isSettled = !isTemporaryResident && hasMovedIn;

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

                    // Vanilla's own death check filters on nothing but being a living citizen, so a household on its
                    // way out of the city is still losing people here.
                    AccumulateDeathChance(resident.m_Citizen, citizen, ref stats);

                    if (isSettled)
                    {
                        stats.m_SettledResidentCount++;
                        stats.m_HealthSum += citizen.m_Health;
                        AccumulateEligibility(resident.m_Citizen, citizen, hasCityModifiers, cityModifiers, ref stats);
                    }
                }

                bool hasWealth = m_Resources.TryGetBuffer(household, out DynamicBuffer<Resources> resources);
                if (isTemporaryResident || !hasWealth)
                {
                    return;
                }
                stats.m_WealthSum += EconomyUtils.GetHouseholdTotalWealth(householdData, resources);
                stats.m_IncomeSum += householdData.m_Income;
                stats.m_HouseholdCount++;
            }

            // Adds one settled resident's chance of entering each school level to the totals.
            private void AccumulateEligibility(
                Entity resident,
                Citizen citizen,
                bool hasCityModifiers,
                DynamicBuffer<CityModifier> cityModifiers,
                ref DistrictStats stats)
            {
                /*
                    Eligibility is a chance rather than a fact, so vanilla adds up each citizen's odds of applying and
                    rounds the total up. A citizen chasing a job is left out entirely, and one already enrolled counts as
                    a certainty for their own level and nothing else.

                    HasJobSeeker is enableable and sits on a citizen whether or not they're looking for work, so its
                    enabled bit has to be asked for separately; HasComponent alone makes every citizen look like a seeker.
                */
                bool isSeekingJob = m_JobSeekers.HasComponent(resident) && m_JobSeekers.IsComponentEnabled(resident);
                if (!m_Education.m_Valid || isSeekingJob)
                {
                    return;
                }

                if (m_Students.TryGetComponent(resident, out Game.Citizens.Student student))
                {
                    AddEligible(ref stats, student.m_Level, 1f);
                    AddEnrolled(ref stats, student.m_Level);
                    return;
                }

                CitizenAge age = citizen.GetAge();
                if (age == CitizenAge.Child)
                {
                    int ageInDays = TimeSystem.GetDay(m_Education.m_SimulationFrame, m_Education.m_TimeData) - citizen.m_BirthDay;
                    if (ageInDays >= kElementaryEligibleAgeDays)
                    {
                        AddEligible(ref stats, (int)SchoolLevel.Elementary, 1f);
                    }

                    return;
                }

                // Only the odds of applying are weighed against the city's modifiers, so a city with none still
                // reports its students and school-age children.
                if (!hasCityModifiers)
                {
                    return;
                }

                int finished = citizen.GetEducationLevel();
                bool isWorker = m_Workers.HasComponent(resident);
                float willingness = citizen.GetPseudoRandom(CitizenPseudoRandom.StudyWillingness).NextFloat();

                if (finished == (int)SchoolLevel.Elementary && age <= CitizenAge.Adult)
                {
                    AddEligible(
                        ref stats,
                        (int)SchoolLevel.HighSchool,
                        EnteringProbability(SchoolLevel.HighSchool, age, isWorker, citizen, willingness, cityModifiers));
                    return;
                }

                // A citizen out of high school picks between university and college, so the college odds are only
                // whatever is left once university has taken its share.
                if (finished == (int)SchoolLevel.HighSchool && citizen.GetFailedEducationCount() < kMaxFailedEducationAttempts)
                {
                    float university =
                        EnteringProbability(SchoolLevel.University, age, isWorker, citizen, willingness, cityModifiers);
                    AddEligible(ref stats, (int)SchoolLevel.University, university);
                    AddEligible(
                        ref stats,
                        (int)SchoolLevel.College,
                        (1f - university)
                            * EnteringProbability(SchoolLevel.College, age, isWorker, citizen, willingness, cityModifiers));
                }
            }

            private static void AddEligible(ref DistrictStats stats, int schoolLevel, float chance)
            {
                if (DistrictStats.TryGetSchoolLane(schoolLevel, out int lane))
                {
                    stats.m_EligibleSums[lane] += chance;
                }
            }

            private static void AddEnrolled(ref DistrictStats stats, int schoolLevel)
            {
                if (DistrictStats.TryGetSchoolLane(schoolLevel, out int lane))
                {
                    stats.m_EnrolledCounts[lane]++;
                }
            }

            // The chance vanilla gives this citizen of applying to the given school level.
            private float EnteringProbability(
                SchoolLevel schoolLevel,
                CitizenAge age,
                bool isWorker,
                Citizen citizen,
                float willingness,
                DynamicBuffer<CityModifier> cityModifiers) =>
                ApplyToSchoolSystem.GetEnteringProbability(
                    age,
                    isWorker,
                    (int)schoolLevel,
                    citizen.m_WellBeing,
                    willingness,
                    cityModifiers,
                    ref m_Education.m_Parameters);

            // Adds one living resident's chance of dying today to the totals.
            private void AccumulateDeathChance(Entity resident, Citizen citizen, ref DistrictStats stats)
            {
                if (!m_Deathcare.m_Valid)
                {
                    return;
                }

                stats.m_DeathRateResidentCount++;

                /*
                    The two causes run on different clocks: illness is rolled afresh every update slice, so a day of it is
                    that many slices' worth, while old age is settled once a day. They're also read as independent, even
                    though the game only rolls for illness once old age has spared the citizen; the overlap is far inside
                    the rounding of a whole-body figure.
                */
                stats.m_DeathRateSum += OldAgeChancePerDay(citizen)
                    + (DeathCheckSystem.kUpdatesPerDay * IllnessChance(resident, citizen));
            }

            /*
                A resident's chance of dying of old age today. The death-rate curve isn't a chance but the threshold a
                number the citizen has carried since birth is compared against, and they die the moment the curve climbs
                past it. So today's cost is how far the curve climbs over the day, as a share of the range a still-living
                citizen's number can be in: above where the curve stands now, and below the top.
            */
            private float OldAgeChancePerDay(Citizen citizen)
            {
                // The game centres a citizen's age on the middle of the day it's checking, not the boundary.
                float ageInDays = citizen.GetAgeInDays(m_Deathcare.m_SimulationFrame, m_Deathcare.m_TimeData)
                    + m_Deathcare.m_NormalizedTime
                    - 0.5f;
                float lifetimeDays = m_Deathcare.m_DaysPerYear * DeathCheckSystem.kMaxAgeInGameYear;

                float today = DeathThreshold(ageInDays / lifetimeDays);
                float survivingRange = 1f - today;
                if (survivingRange <= math.EPSILON)
                {
                    return 1f;
                }

                float tomorrow = DeathThreshold((ageInDays + 1f) / lifetimeDays);
                return math.saturate(math.max(0f, tomorrow - today) / survivingRange);
            }

            // Where the death-rate curve this save is being run against stands at a share of a full lifetime.
            private float DeathThreshold(float lifetimeShare) =>
                math.saturate(
                    m_Deathcare.m_UseNewCurve
                        ? m_Deathcare.m_Parameters.m_DeathRate.Evaluate(lifetimeShare)
                        : m_Deathcare.m_Parameters.m_LegacyDeathRate.Evaluate(lifetimeShare));

            /*
                A sick or injured resident's chance of dying of it in one update slice, which the game works out from
                how far their health has fallen: every full ten points lost is worth more than the last, on top of a
                floor however mild the illness. The game kills the citizen when a draw below the scale comes out at or
                under their figure, so the figure itself counts as a losing draw.
            */
            private float IllnessChance(Entity resident, Citizen citizen)
            {
                const int kIllnessChanceFloor = 8;
                const int kIllnessChanceScale = 1000;

                if (!m_HealthProblems.TryGetComponent(resident, out HealthProblem problem)
                    || (problem.m_Flags & (HealthProblemFlags.Sick | HealthProblemFlags.Injured)) == 0)
                {
                    return 0f;
                }

                int healthLost = 10 - citizen.m_Health / 10;
                return ((healthLost * healthLost) + kIllnessChanceFloor + 1)
                    / (float)(DeathCheckSystem.kUpdatesPerDay * kIllnessChanceScale);
            }
        }
    }
}

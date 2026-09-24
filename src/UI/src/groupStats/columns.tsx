import { Unit } from "cs2/l10n"
import { Icon, Tooltip } from "cs2/ui"

import { DataColumn } from "../components/DataTable"
import { Occupancy, PlacesTooltip, hasCapacity, occupancyShare } from "../components/OccupancyStats"
import { StatValue, ThresholdValue } from "../components/StatValue"
import { kGenericType, kNoValue, kTypeIcons } from "../constants"
import { AssignedBuilding, DistrictMember } from "../types"
import { VanillaLabel, VanillaLocale, happinessThreshold, wealthThreshold } from "../utils/locale"

import css from "./columns.module.scss"
import { GameText, ModText } from "./labels"
import { placesOf } from "./places"

// Ranks rows by one of their figures.
const byStat = <T,>(of: (row: T) => number) => (a: T, b: T) => of(a) - of(b)

const districtColumn: DataColumn<DistrictMember> = {
    id: "district",
    label: <GameText label={VanillaLocale.districtsColumn} />,
    layout: "name",
    compare: (a, b) => a.name.localeCompare(b.name),
    render: (member) => member.name,
    renderTotal: () => <GameText label={VanillaLocale.total} />,
}

const populationColumn: DataColumn<DistrictMember> = {
    id: "population",
    label: <GameText label={VanillaLocale.populationColumn} />,
    descendingFirst: true,
    compare: byStat((member) => member.population),
    render: (member) => <StatValue value={member.population} unit={Unit.Integer} />,
}

const happinessColumn: DataColumn<DistrictMember> = {
    id: "happiness",
    label: <GameText label={VanillaLocale.happinessColumn} />,
    descendingFirst: true,
    compare: byStat((member) => member.happiness),
    render: (member) => <ThresholdValue label={happinessThreshold(member.happiness)} />,
}

// Average household income has no vanilla band to bucket it into, so it reads as currency on the wealth values.
const IncomeTooltip = ({ income }: { income: number }) => (
    <>
        <div>
            <GameText label={VanillaLocale.incomeColumn} />
        </div>
        <div>
            <StatValue value={income} unit={Unit.MoneyPerMonth} />
        </div>
    </>
)

const wealthColumn: DataColumn<DistrictMember> = {
    id: "wealth",
    label: <GameText label={VanillaLocale.wealthColumn} />,
    headerTooltip: (total) => <IncomeTooltip income={total?.income ?? kNoValue} />,
    descendingFirst: true,
    compare: byStat((member) => member.wealth),
    render: (member) => (
        <Tooltip tooltip={<IncomeTooltip income={member.income} />}>
            <div>
                <ThresholdValue label={wealthThreshold(member.wealth)} />
            </div>
        </Tooltip>
    ),
}

const crimeChanceColumn: DataColumn<DistrictMember> = {
    id: "crimeChance",
    label: <GameText label={VanillaLocale.crimeProbability} />,
    descendingFirst: true,
    compare: byStat((member) => member.crimeChance),
    render: (member) => <StatValue value={member.crimeChance} unit={Unit.Percentage} />,
}

const fireRiskColumn: DataColumn<DistrictMember> = {
    id: "fireRisk",
    label: <GameText label={VanillaLocale.fireHazard} />,
    descendingFirst: true,
    compare: byStat((member) => member.fireRisk),
    render: (member) => <StatValue value={member.fireRisk} unit={Unit.Percentage} />,
}

const healthColumn: DataColumn<DistrictMember> = {
    id: "health",
    label: <GameText label={VanillaLocale.averageHealth} />,
    descendingFirst: true,
    compare: byStat((member) => member.health),
    render: (member) => <StatValue value={member.health} unit={Unit.Percentage} />,
}

const activePatientsColumn: DataColumn<DistrictMember> = {
    id: "activePatients",
    label: <GameText label={VanillaLocale.patients} />,
    descendingFirst: true,
    compare: byStat((member) => member.activePatients),
    render: (member) => <StatValue value={member.activePatients} unit={Unit.Integer} />,
}

const deathsPerDayColumn: DataColumn<DistrictMember> = {
    id: "deathsPerDay",
    label: <GameText label={VanillaLocale.deceased} />,
    descendingFirst: true,
    compare: byStat((member) => member.deathsPerDay),
    // intentionally uses IntegerPerMonth because of a bug with BodiesPerMonth between the mod model & game model
    render: (member) => <StatValue value={member.deathsPerDay} unit={Unit.IntegerPerMonth} />,
}

const garbageGenerationColumn: DataColumn<DistrictMember> = {
    id: "garbageGeneration",
    label: <GameText label={VanillaLocale.garbageAccumulation} />,
    descendingFirst: true,
    compare: byStat((member) => member.garbageGeneration),
    render: (member) => <StatValue value={member.garbageGeneration} unit={Unit.WeightPerMonth} />,
}

const eligibleColumn: DataColumn<DistrictMember> = {
    id: "eligible",
    label: <ModText label="eligibleColumnLabel" />,
    descendingFirst: true,
    compare: byStat((member) => member.eligible),
    render: (member) => <StatValue value={member.eligible} unit={Unit.Integer} />,
}

// Residents already in school beside the eligible count, so a district's own coverage shows without weighing it
// against any one group's capacity.
const enrolledColumn: DataColumn<DistrictMember> = {
    id: "enrolled",
    label: <GameText label={VanillaLocale.students} />,
    descendingFirst: true,
    compare: byStat((member) => member.enrolled),
    render: (member) => <StatValue value={member.enrolled} unit={Unit.Integer} />,
}

const kEducationOverview = [districtColumn, populationColumn, eligibleColumn, enrolledColumn]

const mailGenerationColumn: DataColumn<DistrictMember> = {
    id: "mailGeneration",
    label: <GameText label={VanillaLocale.mailAccumulation} />,
    descendingFirst: true,
    compare: byStat((member) => member.mailGeneration),
    render: (member) => <StatValue value={member.mailGeneration} unit={Unit.Integer} />,
}

// The district columns each group type lists, indexed by GroupServiceType - order must match the C# enum.
const kOverviewColumns: DataColumn<DistrictMember>[][] = [
    [districtColumn, populationColumn, happinessColumn, wealthColumn],
    [districtColumn, populationColumn, crimeChanceColumn],
    [districtColumn, populationColumn, fireRiskColumn],
    [districtColumn, populationColumn, healthColumn, activePatientsColumn],
    [districtColumn, populationColumn, deathsPerDayColumn],
    [districtColumn, populationColumn, garbageGenerationColumn],
    kEducationOverview,
    kEducationOverview,
    kEducationOverview,
    kEducationOverview,
    [districtColumn, populationColumn, mailGenerationColumn],
]

// What a group of this type lists about each of its member districts.
export const overviewColumns = (type: number): DataColumn<DistrictMember>[] =>
    kOverviewColumns[type] ?? kOverviewColumns[kGenericType]

// How full one building's own places are, with the places behind that share on hover.
const CapacityCell = ({ building }: { building: AssignedBuilding }) => {
    const occupancy = <Occupancy occupants={building.occupants} capacity={building.capacity} />
    const places = placesOf(building.type)

    if (!hasCapacity(building.capacity) || places === null) {
        return occupancy
    }

    return (
        <Tooltip
            tooltip={
                <PlacesTooltip
                    label={<GameText label={places.label} />}
                    unit={places.unit}
                    claimed={building.occupants}
                    capacity={building.capacity}
                />
            }
        >
            <div>{occupancy}</div>
        </Tooltip>
    )
}

// Prefixes the building's own type icon onto its name, but only for a Civic group - every other
// group's buildings all share its own type already shown by the group's own icon.
const buildingColumn = (groupType: number): DataColumn<AssignedBuilding> => ({
    id: "building",
    label: <GameText label={VanillaLocale.buildingsColumn} />,
    layout: "name",
    compare: (a, b) => a.name.localeCompare(b.name),
    render: (building) =>
        groupType === kGenericType ? (
            <div className={css.buildingTypeNameCell}>
                <Icon
                    src={kTypeIcons[building.type]}
                    className={css.buildingTypeIcon} />

                {building.name}
            </div>
        ) : (
            building.name
        ),
})

// How full each building's own places are, under the game's own name for that type's capacity.
const capacityColumn = (label: VanillaLabel): DataColumn<AssignedBuilding> => ({
    id: "capacity",
    label: <GameText label={label} />,
    descendingFirst: true,
    compare: (a, b) => occupancyShare(a) - occupancyShare(b),
    render: (building) => <CapacityCell building={building} />,
})

// A shelter holds nobody day to day, so this reads out what it can hold rather than an occupancy share.
const shelterCapacityColumn: DataColumn<AssignedBuilding> = {
    id: "capacity",
    label: <GameText label={VanillaLocale.shelterCapacity} />,
    descendingFirst: true,
    compare: byStat((building) => building.capacity),
    render: (building) => (
        <StatValue value={hasCapacity(building.capacity) ? building.capacity : kNoValue} unit={Unit.Integer} />
    ),
}

// What each building works through in a day, as opposed to what it can hold; one that only ever fills up reads as unreported.
const processingColumn = (label: VanillaLabel, unit: Unit): DataColumn<AssignedBuilding> => ({
    id: "processing",
    label: <GameText label={label} />,
    descendingFirst: true,
    compare: byStat((building) => building.processingCapacity),
    render: (building) => (
        <StatValue
            value={hasCapacity(building.processingCapacity) ? building.processingCapacity : kNoValue}
            unit={unit}
        />
    ),
})

const efficiencyColumn: DataColumn<AssignedBuilding> = {
    id: "efficiency",
    label: <GameText label={VanillaLocale.efficiencyColumn} />,
    descendingFirst: true,
    compare: byStat((building) => building.efficiency),
    render: (building) => <StatValue value={building.efficiency} unit={Unit.Percentage} />,
}

const kSchoolBuildings = [capacityColumn(VanillaLocale.studentCapacity), efficiencyColumn]

// The facility columns each group type lists after name, indexed by GroupServiceType - order must match the C# enum.
const kBuildingColumns: DataColumn<AssignedBuilding>[][] = [
    [efficiencyColumn],
    [capacityColumn(VanillaLocale.jailCapacity), efficiencyColumn],
    [shelterCapacityColumn, efficiencyColumn],
    [capacityColumn(VanillaLocale.patientCapacity), efficiencyColumn],
    [
        capacityColumn(VanillaLocale.deceasedStorage),
        processingColumn(VanillaLocale.deceasedProcessingCapacity, Unit.IntegerPerMonth),
        efficiencyColumn,
    ],
    [
        processingColumn(VanillaLocale.garbageProcessingCapacity, Unit.WeightPerMonth),
        capacityColumn(VanillaLocale.garbageStorage),
        efficiencyColumn,
    ],
    kSchoolBuildings,
    kSchoolBuildings,
    kSchoolBuildings,
    kSchoolBuildings,
    [capacityColumn(VanillaLocale.mailStorage), efficiencyColumn],
]

// What a group of this type lists about each of its assigned buildings.
export const buildingsColumns = (type: number): DataColumn<AssignedBuilding>[] => [
    buildingColumn(type),
    ...(kBuildingColumns[type] ?? kBuildingColumns[kGenericType]),
]

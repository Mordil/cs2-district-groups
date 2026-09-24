import { Unit } from "cs2/l10n"

import { DataColumn } from "../components/DataTable"
import { StatValue, ThresholdValue } from "../components/StatValue"
import { kGenericType } from "../constants"
import { AssignedBuilding, DistrictMember } from "../types"
import { VanillaLocale, happinessThreshold, wealthThreshold } from "../utils/locale"

import { GameText, ModText } from "./labels"

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

const wealthColumn: DataColumn<DistrictMember> = {
    id: "wealth",
    label: <GameText label={VanillaLocale.wealthColumn} />,
    descendingFirst: true,
    compare: byStat((member) => member.wealth),
    render: (member) => <ThresholdValue label={wealthThreshold(member.wealth)} />,
}

// Average household income has no vanilla band to bucket it into, so it reads as currency.
const incomeColumn: DataColumn<DistrictMember> = {
    id: "income",
    label: <GameText label={VanillaLocale.incomeColumn} />,
    descendingFirst: true,
    compare: byStat((member) => member.income),
    render: (member) => <StatValue value={member.income} unit={Unit.MoneyPerMonth} />,
}

// The district columns each group type lists, indexed by GroupServiceType - order must match the C# enum.
const kOverviewColumns: DataColumn<DistrictMember>[][] = [
    [districtColumn, populationColumn, happinessColumn, wealthColumn, incomeColumn],
]

// What a group of this type lists about each of its member districts.
export const overviewColumns = (type: number): DataColumn<DistrictMember>[] =>
    kOverviewColumns[type] ?? kOverviewColumns[kGenericType]

const buildingColumn: DataColumn<AssignedBuilding> = {
    id: "building",
    label: <GameText label={VanillaLocale.buildingsColumn} />,
    layout: "name",
    compare: (a, b) => a.name.localeCompare(b.name),
    render: (building) => building.name,
}

const efficiencyColumn: DataColumn<AssignedBuilding> = {
    id: "efficiency",
    label: <GameText label={VanillaLocale.efficiencyColumn} />,
    descendingFirst: true,
    compare: byStat((building) => building.efficiency),
    render: (building) => <StatValue value={building.efficiency} unit={Unit.Percentage} />,
}

// The facility columns each group type lists after name and type, indexed by GroupServiceType - order must match the C# enum.
const kBuildingColumns: DataColumn<AssignedBuilding>[][] = [
    [efficiencyColumn],
]

// What a group of this type lists about each of its assigned buildings.
//
// The type column is built per render, since it ranks and reads out the translated type names.
export const buildingsColumns = (
    type: number,
    typeLabels: string[]
): DataColumn<AssignedBuilding>[] => [
    buildingColumn,
    {
        id: "type",
        label: <ModText label="typeColumnLabel" />,
        layout: "text",
        compare: (a, b) => typeLabels[a.type].localeCompare(typeLabels[b.type]),
        render: (building) => typeLabels[building.type],
    },
    ...(kBuildingColumns[type] ?? kBuildingColumns[kGenericType]),
]

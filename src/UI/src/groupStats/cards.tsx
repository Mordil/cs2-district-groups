import { ReactNode } from "react"

import { Unit } from "cs2/l10n"

import { gameIconSrc, glyphIconSrc, modIconSrc } from "../components/icons"
import { Load, PlacesTooltip, assignedCapacity } from "../components/OccupancyStats"
import { StatValue } from "../components/StatValue"
import { kGenericType } from "../constants"
import { AssignedBuilding, Group } from "../types"
import { VanillaLocale } from "../utils/locale"

import { GameText, ModText } from "./labels"

// What a group's own assigned buildings add up to, which its type's demand figures are weighed against.
export interface GroupCapacity {
    // The places the buildings provide
    places: number
}

// Sums a group's buildings once per card, rather than once per readout that needs them.
export const groupCapacity = (buildings: AssignedBuilding[]): GroupCapacity => ({
    places: assignedCapacity(buildings),
})

// One readout on a group's card, beside its district and building counts.
export interface CardStat {
    icon: string
    // Glyph-style icons have no color of their own to desaturate, so they're tinted instead
    tinted?: boolean
    render: (group: Group, capacity: GroupCapacity) => ReactNode
    tooltip: (group: Group, capacity: GroupCapacity) => ReactNode
}

interface ReadingProps {
    icon: string
    unit: Unit
    // The figure the group reports directly
    of: (group: Group) => number
    label: ReactNode
}

// A figure the group reports directly, under the name the game or the mod gives it.
const reading = ({ icon, unit, of, label }: ReadingProps): CardStat => ({
    icon,
    render: (group) => <StatValue value={of(group)} unit={unit} />,
    tooltip: () => label,
})

interface LoadProps {
    icon: string
    tinted?: boolean
    unit: Unit
    // What is calling on the group, and which of the group's own figures that is weighed against
    demand: (group: Group, capacity: GroupCapacity) => number
    supply: (capacity: GroupCapacity) => number
    label: ReactNode
}

// How hard the group's own capacity is being leaned on, with the two figures behind that share on hover.
const load = ({ icon, tinted, unit, demand, supply, label }: LoadProps): CardStat => ({
    icon,
    tinted,
    render: (group, capacity) => <Load demand={demand(group, capacity)} capacity={supply(capacity)} />,
    tooltip: (group, capacity) => (
        <PlacesTooltip label={label} unit={unit} claimed={demand(group, capacity)} capacity={supply(capacity)} />
    ),
})

const places = (capacity: GroupCapacity) => capacity.places

// What each group type reads out on its card, indexed by GroupServiceType - order must match the C# enum.
const kCardStats: CardStat[][] = [
    [
        reading({
            icon: gameIconSrc("Population"),
            unit: Unit.Integer,
            of: (group) => group.population,
            label: <ModText label="metadataPopulationTooltip" />,
        }),
    ],
    [
        reading({
            icon: glyphIconSrc("Prisoner"),
            unit: Unit.Percentage,
            of: (group) => group.crimeChance,
            label: <GameText label={VanillaLocale.averageCrimeProbability} />,
        }),
    ],
    [
        reading({
            icon: gameIconSrc("Flame"),
            unit: Unit.Percentage,
            of: (group) => group.fireRisk,
            label: <GameText label={VanillaLocale.averageFireHazard} />,
        }),
        load({
            icon: modIconSrc("throughput"),
            tinted: true,
            unit: Unit.Integer,
            demand: (group) => group.population,
            supply: places,
            label: <GameText label={VanillaLocale.shelterCapacity} />,
        }),
    ],
    [
        reading({
            icon: gameIconSrc("Wellbeing"),
            unit: Unit.Percentage,
            of: (group) => group.health,
            label: <GameText label={VanillaLocale.averageHealth} />,
        }),
    ],
]

// What a group of this type reads out on its card beyond its district and building counts.
export const cardStats = (type: number): CardStat[] => kCardStats[type] ?? kCardStats[kGenericType]

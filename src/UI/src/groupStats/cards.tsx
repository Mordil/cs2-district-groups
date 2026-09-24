import { ReactNode } from "react"

import { Unit } from "cs2/l10n"

import { gameIconSrc, glyphIconSrc, modIconSrc } from "../components/icons"
import {
    Load,
    PlacesTooltip,
    assignedCapacity,
    assignedOccupants,
    assignedProcessingCapacity,
} from "../components/OccupancyStats"
import { StatValue, ThresholdValue } from "../components/StatValue"
import { kGenericType } from "../constants"
import { AssignedBuilding, Group } from "../types"
import { VanillaLocale, wealthThreshold } from "../utils/locale"

import { GameText } from "./labels"

// What a group's own assigned buildings add up to, which its type's demand figures are weighed against.
export interface GroupCapacity {
    // The places the buildings provide, and how many of those places are already taken
    places: number
    taken: number
    // What they work through in a day, for the types whose own demand figure is a rate
    processing: number
}

// Sums a group's buildings once per card, rather than once per readout that needs them.
export const groupCapacity = (buildings: AssignedBuilding[]): GroupCapacity => ({
    places: assignedCapacity(buildings),
    taken: assignedOccupants(buildings),
    processing: assignedProcessingCapacity(buildings),
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
    tinted?: boolean
    unit: Unit
    // The figure the group reports directly
    of: (group: Group) => number
    label: ReactNode
}

// A figure the group reports directly, under the name the game or the mod gives it.
const reading = ({ icon, tinted, unit, of, label }: ReadingProps): CardStat => ({
    icon,
    tinted,
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
const processing = (capacity: GroupCapacity) => capacity.processing

const kEducationCard: CardStat[] = [
    reading({
        icon: glyphIconSrc("Student"),
        tinted: true,
        unit: Unit.Integer,
        of: (group) => group.enrolled,
        label: <GameText label={VanillaLocale.students} />,
    }),
    load({
        icon: modIconSrc("throughput"),
        tinted: true,
        unit: Unit.Integer,
        demand: (group) => group.eligible,
        supply: places,
        label: <GameText label={VanillaLocale.demand} />,
    }),
]

// What each group type reads out on its card, indexed by GroupServiceType - order must match the C# enum.
const kCardStats: CardStat[][] = [
    [
        reading({
            icon: gameIconSrc("Population"),
            unit: Unit.Integer,
            of: (group) => group.population,
            label: <GameText label={VanillaLocale.populationColumn} />,
        }),
        {
            icon: gameIconSrc("CitizenWealth"),
            render: (group) => <ThresholdValue label={wealthThreshold(group.wealth)} />,
            tooltip: () => <GameText label={VanillaLocale.averageHouseholdWealth} />,
        },
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
    // A crematorium keeps up at a rate while a cemetery fills plots it never gets back, so deathcare weighs both.
    [
        load({
            icon: gameIconSrc("Deathcare"),
            unit: Unit.Integer,
            demand: (_group, capacity) => capacity.taken,
            supply: places,
            label: <GameText label={VanillaLocale.deceased} />,
        }),
        load({
            icon: modIconSrc("throughput"),
            tinted: true,
            unit: Unit.BodiesPerMonth,
            demand: (group) => group.deathsPerDay,
            supply: processing,
            label: <GameText label={VanillaLocale.deceasedProcessingCapacity} />,
        }),
    ],
    [
        load({
            icon: modIconSrc("throughput"),
            tinted: true,
            unit: Unit.WeightPerMonth,
            demand: (group) => group.garbageGeneration,
            supply: processing,
            label: <GameText label={VanillaLocale.garbageProcessingCapacity} />,
        }),
    ],
    kEducationCard,
    kEducationCard,
    kEducationCard,
    kEducationCard,
    [
        load({
            icon: gameIconSrc("PostService"),
            unit: Unit.Integer,
            demand: (group) => group.mailGeneration,
            supply: places,
            label: <GameText label={VanillaLocale.storedMail} />,
        }),
    ],
]

// What a group of this type reads out on its card beyond its district and building counts.
export const cardStats = (type: number): CardStat[] => kCardStats[type] ?? kCardStats[kGenericType]

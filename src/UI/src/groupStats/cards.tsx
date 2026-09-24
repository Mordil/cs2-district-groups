import { ReactNode } from "react"

import { Unit } from "cs2/l10n"

import { gameIconSrc, glyphIconSrc } from "../components/icons"
import { StatValue } from "../components/StatValue"
import { kGenericType } from "../constants"
import { Group } from "../types"
import { VanillaLocale } from "../utils/locale"

import { GameText, ModText } from "./labels"

// One readout on a group's card, beside its district and building counts.
export interface CardStat {
    icon: string
    render: (group: Group) => ReactNode
    tooltip: (group: Group) => ReactNode
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
]

// What a group of this type reads out on its card beyond its district and building counts.
export const cardStats = (type: number): CardStat[] => kCardStats[type] ?? kCardStats[kGenericType]

import { ReactNode } from "react"

import { LocalizedFraction, LocalizedNumber, Unit } from "cs2/l10n"

import { kNoValue, kNoValueText } from "../constants"

// Whether a building reported any places at all.
export const hasCapacity = (capacity: number) => capacity > 0

interface OccupancyProps {
    // Places taken, and the places there are for them
    occupants: number
    capacity: number
}

// How full a building's own places are, so one at its limit reads 100%, or kNoValue, which sorts below every real share.
export const occupancyShare = ({ occupants, capacity }: OccupancyProps) =>
    hasCapacity(capacity) && occupants >= 0 ? Math.min(1, occupants / capacity) : kNoValue

// A share read out as a whole percent, or a placeholder for kNoValue.
const SharePercentage = ({ share }: { share: number }) =>
    share === kNoValue ? (
        <>{kNoValueText}</>
    ) : (
        <LocalizedNumber value={Math.round(100 * share)} unit={Unit.Percentage} />
    )

// The share of a building's own places that are taken, or a placeholder for one with none to report.
export const Occupancy = (places: OccupancyProps) => <SharePercentage share={occupancyShare(places)} />

interface PlacesTooltipProps {
    // What the two figures are being read out as
    label: ReactNode
    // How many of the places are called for, and how many there are
    claimed: number
    capacity: number
    unit?: Unit
}

// What a places readout was worked out from, under the name given to that figure.
export const PlacesTooltip = ({ label, claimed, capacity, unit = Unit.Integer }: PlacesTooltipProps) => (
    <>
        <div>{label}</div>
        {hasCapacity(capacity) && claimed >= 0 && (
            <div>
                <LocalizedFraction value={claimed} total={capacity} unit={unit} />
            </div>
        )}
    </>
)

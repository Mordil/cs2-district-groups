import { ReactNode } from "react"

import { LocalizedFraction, LocalizedNumber, Unit } from "cs2/l10n"

import { kNoValue, kNoValueText } from "../constants"
import { AssignedBuilding } from "../types"

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

// A building with no efficiency left - no power, no water, no staff, budget switched off - can fill none of its
// places, the same exclusion the game's own infoviews make. One the game reports no efficiency for is only unmeasured.
const isStalled = (building: AssignedBuilding) => building.efficiency !== kNoValue && building.efficiency <= 0

interface LoadProps {
    // Everything calling on a group's places, and the places there are for it
    demand: number
    capacity: number
}

// How hard a group's capacity is leaned on: demand over supply, so 100% is break-even and 112% is twelve percent past it.
const loadShare = ({ demand, capacity }: LoadProps) =>
    hasCapacity(capacity) && demand >= 0 ? demand / capacity : kNoValue

// The share of a group's places its demand calls for, or a placeholder where it has none.
export const Load = (places: LoadProps) => <SharePercentage share={loadShare(places)} />

// The places a group's own buildings provide, leaving out any that has stalled.
export const assignedCapacity = (buildings: AssignedBuilding[]) =>
    buildings.reduce(
        (total, building) =>
            hasCapacity(building.capacity) && !isStalled(building) ? total + building.capacity : total,
        0
    )

// The places a group's own buildings have taken, over the same buildings assignedCapacity counts the places of.
export const assignedOccupants = (buildings: AssignedBuilding[]) =>
    buildings.reduce(
        (total, building) =>
            hasCapacity(building.capacity) && !isStalled(building) && building.occupants >= 0
                ? total + building.occupants
                : total,
        0
    )

// What a group's own buildings work through in a day, leaving out any that has stalled.
export const assignedProcessingCapacity = (buildings: AssignedBuilding[]) =>
    buildings.reduce(
        (total, building) =>
            hasCapacity(building.processingCapacity) && !isStalled(building)
                ? total + building.processingCapacity
                : total,
        0
    )

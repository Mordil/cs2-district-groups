import { Color } from "cs2/bindings"
import { Entity } from "cs2/utils"

// A district that belongs to a group, with the per-district numbers its overview row shows
export interface DistrictMember extends ResidentStats {
    entity: Entity
    name: string
}

/*
    The resident figures a district, or a whole group, reads out.

    Happiness and wealth arrive as the ordinal of the band their average landed in rather than as a
    raw average, and are kNoThreshold when there were no residents to average.
*/
export interface ResidentStats {
    population: number
    happiness: number
    wealth: number
}

// A service building assigned to a group, with the per-building numbers its buildings row shows
export interface AssignedBuilding {
    entity: Entity
    name: string
    type: number
    // Whole-percent efficiency, or kUnknownEfficiency when the building reports none
    efficiency: number
}

// A service building of the currently filtered type, with the assignment state its row needs
export interface ServiceBuilding {
    entity: Entity
    name: string
    type: number
    hasAssignment: boolean
    assignedGroup: Entity
    assignedGroupName: string
    // Locale id for the asset's display name (e.g. "Assets.NAME[PoliceStation01]"),
    // with the raw prefab name as the fallback when nothing resolves it.
    assetNameId: string
    assetName: string
}

// The adjustable value a policy carries on top of being switched on or off
export interface PolicySlider {
    min: number
    max: number
    default: number
    step: number
    // A cs2/l10n Unit name, or "" when the game reports none for the policy
    unit: string
}

// How one member district has a policy set, as its row inside an expanded policy reads it
export interface DistrictPolicyState {
    entity: Entity
    name: string
    active: boolean
    // The value the district carries; only meaningful when the policy has a slider
    value: number
}

// A district policy, with how every district of the focused group currently has it set
export interface GroupPolicy {
    // The policy prefab, which is what every policy write names as its subject
    entity: Entity
    // The hash the game keys Policy.TITLE and Policy.DESCRIPTION on
    id: string
    icon: string
    slider: PolicySlider | null
    // Empty when no group is focused, or when the focused group has no districts
    districts: DistrictPolicyState[]
}

export interface Group extends ResidentStats {
    entity: Entity
    name: string
    type: number
    color: Color
    members: DistrictMember[]
    buildings: AssignedBuilding[]
}

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

export interface Group extends ResidentStats {
    entity: Entity
    name: string
    type: number
    color: Color
    members: DistrictMember[]
    buildings: AssignedBuilding[]
}

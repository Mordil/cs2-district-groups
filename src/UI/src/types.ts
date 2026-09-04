import { Color } from "cs2/bindings"
import { Entity } from "cs2/utils"

export interface NamedEntity {
    entity: Entity
    name: string
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

export interface Group {
    entity: Entity
    name: string
    type: number
    color: Color
    assignedBuildingCount: number
    population: number
    members: NamedEntity[]
}

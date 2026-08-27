import { Color } from "cs2/bindings"
import { Entity } from "cs2/utils"

export interface NamedEntity {
    entity: Entity
    name: string
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

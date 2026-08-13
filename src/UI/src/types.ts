import { Entity } from "cs2/utils"

export interface NamedEntity {
    entity: Entity
    name: string
}

export interface Group {
    entity: Entity
    name: string
    type: number
    assignedBuildingCount: number
    members: NamedEntity[]
}

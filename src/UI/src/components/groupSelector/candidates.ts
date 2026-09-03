import { Color } from "cs2/bindings"
import { Entity, entityEquals } from "cs2/utils"

export const kGenericGroupType = 0

export interface GroupOption {
    entity: Entity
    name: string
    type: number
    color: Color
}

export interface GroupSection {
    type: number
    options: GroupOption[]
}

export const eligibleGroups = (
    groups: GroupOption[],
    buildingType: number,
    assignedGroup?: Entity,
): GroupOption[] =>
    groups.filter((group) =>
        (buildingType === kGenericGroupType
            || group.type === buildingType
            || group.type === kGenericGroupType)
        && !entityEquals(group.entity, assignedGroup))

// One section per candidate type, each alphabetized
// The building's own type and Generic always get a section, even if empty.
export const groupCandidatesByType = (candidates: GroupOption[], buildingType: number): GroupSection[] => {
    const byType = new Map<number, GroupOption[]>()
    byType.set(buildingType, [])
    byType.set(kGenericGroupType, [])
    for (const candidate of candidates) {
        const options = byType.get(candidate.type)
        if (options) {
            options.push(candidate)
        } else {
            byType.set(candidate.type, [candidate])
        }
    }

    const types = [...byType.keys()].sort((a, b) => {
        const aMatches = a === buildingType
        const bMatches = b === buildingType
        if (aMatches !== bMatches) {
            return aMatches ? -1 : 1
        }
        return a - b
    })

    return types.map((type) => ({
        type,
        options: [...byType.get(type)!].sort((a, b) => a.name.localeCompare(b.name)),
    }))
}

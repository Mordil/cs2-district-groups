import { trigger } from "cs2/api"
import { Color } from "cs2/bindings"
import { Entity } from "cs2/utils"

import mod from "../mod.json"

function createTrigger<Args extends any[] = []>(name: string): (...args: Args) => void {
    return (...args: Args) => trigger(mod.id, name, ...args)
}

export const log = createTrigger<[level: string, message: string]>("log")

// Overlay and its footer toggles
export const setOverlay = createTrigger<[open: boolean]>("setOverlay")
export const setOverlayFilter = createTrigger<[type: number]>("setOverlayFilter")
export const setAreasVisible = createTrigger<[visible: boolean]>("setAreasVisible")
export const setShowOverlay = createTrigger<[show: boolean]>("setShowOverlay")
export const setShowServiceBuildings = createTrigger<[show: boolean]>("setShowServiceBuildings")
export const setHideAssignedBuildings = createTrigger<[hide: boolean]>("setHideAssignedBuildings")

// Narrows the overlay and the service-building markers to a single group while its details are on screen
export const setFocusedGroup = createTrigger<[group: Entity]>("setFocusedGroup")
export const clearFocusedGroup = createTrigger("clearFocusedGroup")

// Group management tab
export const createGroup = createTrigger("createGroup")
export const deleteGroup = createTrigger<[group: Entity]>("deleteGroup")
export const renameGroup = createTrigger<[group: Entity, name: string]>("renameGroup")
export const setGroupType = createTrigger<[group: Entity, type: number]>("setGroupType")
export const setGroupColor = createTrigger<[group: Entity, color: Color]>("setGroupColor")
export const removeMember = createTrigger<[group: Entity, district: Entity]>("removeMember")
export const toggleDistrictSelection = createTrigger<[group: Entity]>("toggleDistrictSelection")

/*
    Group info panel, policies tab. A policy belongs to the district, so a group-wide write is a
    fan-out over its members - any other group sharing one of those districts sees the change too.
*/
export const setGroupPolicyActive =
    createTrigger<[group: Entity, policy: Entity, active: boolean]>("setGroupPolicyActive")
export const setGroupPolicyValue =
    createTrigger<[group: Entity, policy: Entity, value: number]>("setGroupPolicyValue")
export const setDistrictPolicyActive =
    createTrigger<[district: Entity, policy: Entity, active: boolean]>("setDistrictPolicyActive")
export const setDistrictPolicyValue =
    createTrigger<[district: Entity, policy: Entity, value: number]>("setDistrictPolicyValue")

// Building assignments tab.
export const assignBuildingGroup = createTrigger<[building: Entity, group: Entity]>("assignBuildingGroup")
export const unassignBuildingGroup = createTrigger<[building: Entity]>("unassignBuildingGroup")

// Info-panel section. Both act on whatever building the info panel has selected.
export const assignGroup = createTrigger<[group: Entity]>("assignGroup")
export const unassignGroup = createTrigger("unassignGroup")

import { bindValue } from "cs2/api"
import { Entity } from "cs2/utils"
import mod from "../../mod.json"
import { Group } from "../types"

export const groups$ = bindValue<Group[]>(mod.id, "groups", [])
export const areasVisible$ = bindValue<boolean>(mod.id, "areasVisible", false)
export const showOverlay$ = bindValue<boolean>(mod.id, "showOverlay", true)
export const showServiceBuildings$ = bindValue<boolean>(mod.id, "showServiceBuildings", false)
export const areaToolActive$ = bindValue<boolean>(mod.id, "areaToolActive", false)
export const overlayVisible$ = bindValue<boolean>(mod.id, "overlayVisible", false)
export const cameraModeActive$ = bindValue<boolean>(mod.id, "cameraModeActive", false)
export const selectingGroup$ = bindValue<Entity>(mod.id, "selectingGroup", { index: 0, version: 0 })

import { bindValue } from "cs2/api"
import { Entity } from "cs2/utils"
import mod from "../../mod.json"
import { Group } from "../types"

export const groups$ = bindValue<Group[]>(mod.id, "groups", [])
export const areasVisible$ = bindValue<boolean>(mod.id, "areasVisible", false)
export const showOverlay$ = bindValue<boolean>(mod.id, "showOverlay", true)
export const selectingGroup$ = bindValue<Entity>(mod.id, "selectingGroup", { index: 0, version: 0 })

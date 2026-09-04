import { bindValue } from "cs2/api"
import { Entity } from "cs2/utils"
import mod from "../mod.json"
import { Group, ServiceBuilding } from "./types"

// Every binding the C# side exposes to the UI.

// Every group, in creation order
export const groups$ = bindValue<Group[]>(mod.id, "groups", [])

// Every service building that can carry a group assignment
export const serviceBuildings$ = bindValue<ServiceBuilding[]>(mod.id, "serviceBuildings", [])

// The group whose district selection is currently active, or the null entity when none is
export const selectingGroup$ = bindValue<Entity>(mod.id, "selectingGroup", { index: 0, version: 0 })

// Display toggles, mirrored by the main panel's footer
export const showOverlay$ = bindValue<boolean>(mod.id, "showOverlay", true)
export const showServiceBuildings$ = bindValue<boolean>(mod.id, "showServiceBuildings", false)
export const areasVisible$ = bindValue<boolean>(mod.id, "areasVisible", false)

// True while the overlay is actually on screen, which C# can revoke out from under us
export const overlayVisible$ = bindValue<boolean>(mod.id, "overlayVisible", false)

// The district area tool wants the same screen space our panel occupies
export const areaToolActive$ = bindValue<boolean>(mod.id, "areaToolActive", false)

// Every other reason C# has decided our panel doesn't belong on screen right now
export const shouldDismissPanel$ = bindValue<boolean>(mod.id, "shouldDismissPanel", false)

// True when the currently selected entity has an active district-group assignment
export const selectedBuildingHasGroupAssignment$ = bindValue<boolean>(
    mod.id,
    "selectedBuildingHasGroupAssignment",
    false
)

// True only in a Debug build of the C# side
export const isDebugBuild$ = bindValue<boolean>(mod.id, "isDebugBuild", false)

import { ModRegistrar } from "cs2/modding"
import { DistrictGroupSectionComponent } from "serviceBuildingAssignmentSection"
import { GroupManager } from "modMenuButton"
import { KitchenSinkDebugPanel } from "debugKitchenSink"
import { wrapVanillaDistrictsSection } from "districtsSectionOverride"

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopRight", KitchenSinkDebugPanel)
    moduleRegistry.append("GameTopLeft", GroupManager)
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        DistrictGroupSectionComponent
    )
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        wrapVanillaDistrictsSection
    )
}

export default register

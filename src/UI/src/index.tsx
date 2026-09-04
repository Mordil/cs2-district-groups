import { ModRegistrar } from "cs2/modding"

import { KitchenSinkDebugPanel } from "Debug/KitchenSink"
import { wrapVanillaDistrictsSection } from "InfoPanel/DistrictsSectionOverride"
import { DistrictGroupSectionComponent } from "InfoPanel/ServiceBuildingAssignmentSection"
import { GroupManager } from "ModMenuButton"

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

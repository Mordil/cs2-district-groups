import { ModRegistrar } from "cs2/modding"

import { KitchenSinkDebugPanel } from "Debug/KitchenSink"
import { GroupMembershipSectionComponent } from "DistrictInfoPanel/GroupMembershipSection"
import { GroupManager } from "ModMenuButton"
import { wrapVanillaDistrictsSection } from "ServiceInfoPanel/DistrictsSectionOverride"
import { DistrictGroupSectionComponent } from "ServiceInfoPanel/ServiceBuildingAssignmentSection"

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
        GroupMembershipSectionComponent
    )
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        wrapVanillaDistrictsSection
    )
}

export default register

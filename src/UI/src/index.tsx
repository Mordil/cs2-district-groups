import { ModRegistrar } from "cs2/modding"
import { DistrictGroupSectionComponent } from "serviceBuildingAssignmentSection"
import { GroupManager } from "modMenuButton"
import { FpsCounter } from "fpsCounter"

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", GroupManager)
    moduleRegistry.append("GameTopLeft", FpsCounter)
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        DistrictGroupSectionComponent
    )
}

export default register

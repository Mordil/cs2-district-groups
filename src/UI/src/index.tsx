import { ModRegistrar } from "cs2/modding";
import { DistrictGroupSectionComponent } from "mods/districtGroupSection";
import { GroupManager } from "mods/groupManagerPanel";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", GroupManager);
    moduleRegistry.extend(
        "game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx",
        "selectedInfoSectionComponents",
        DistrictGroupSectionComponent
    );
};

export default register;

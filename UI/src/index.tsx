import { ModRegistrar } from "cs2/modding";
import { MultiDistrictButton } from "mods/multiDistrictButton";
import mod from "../mod.json";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", MultiDistrictButton);
    console.log(mod.id + " UI module registrations completed.");
};

export default register;

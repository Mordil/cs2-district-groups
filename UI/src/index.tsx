import { ModRegistrar } from "cs2/modding";
import { GroupManager } from "mods/groupManagerPanel";
import mod from "../mod.json";

const register: ModRegistrar = (moduleRegistry) => {
    moduleRegistry.append("GameTopLeft", GroupManager);
    console.log(mod.id + " UI module registrations completed.");
};

export default register;

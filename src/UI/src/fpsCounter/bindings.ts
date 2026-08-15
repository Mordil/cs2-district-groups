import { bindValue } from "cs2/api"
import mod from "../../mod.json"

// UI-only (no persistence)
export const debugShowFpsCounter$ = bindValue<boolean>(mod.id, "debugShowFpsCounter", false)

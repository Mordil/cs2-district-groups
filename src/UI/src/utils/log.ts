import { trigger } from "cs2/api"
import mod from "../../mod.json"

type LogLevel = "debug" | "info" | "warn" | "error" | "critical"

function log(level: LogLevel, message: string): void {
    trigger(mod.id, "log", level, message)
}

export const logger = {
    debug: (message: string) => log("debug", message),
    info: (message: string) => log("info", message),
    warn: (message: string) => log("warn", message),
    error: (message: string) => log("error", message),
    critical: (message: string) => log("critical", message),
}

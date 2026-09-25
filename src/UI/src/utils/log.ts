import { debugLogging$ } from "../bindings"
import { log as logTrigger } from "../triggers"

type LogLevel = "debug" | "info" | "warn" | "error" | "critical"

// Built lazily by the caller, so a discarded Debug line never pays for its own interpolation.
type LazyMessage = string | (() => string)

/*
    Every call crosses into C# through a trigger, which is far more expensive than the string it carries.
    Debug lines are dropped here rather than on the far side so a disabled level costs nothing at all
 */
const isDebugEnabled = debugLogging$.subscribe()

function log(level: LogLevel, message: LazyMessage): void {
    logTrigger(level, typeof message === "function" ? message() : message)
}

export const logger = {
    debug: (message: LazyMessage) => {
        if (isDebugEnabled.value) {
            log("debug", message)
        }
    },
    info: (message: LazyMessage) => log("info", message),
    warn: (message: LazyMessage) => log("warn", message),
    error: (message: LazyMessage) => log("error", message),
    critical: (message: LazyMessage) => log("critical", message),
}

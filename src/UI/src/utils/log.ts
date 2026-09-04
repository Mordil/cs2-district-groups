import { log as logTrigger } from "../triggers"

type LogLevel = "debug" | "info" | "warn" | "error" | "critical"

function log(level: LogLevel, message: string): void {
    logTrigger(level, message)
}

export const logger = {
    debug: (message: string) => log("debug", message),
    info: (message: string) => log("info", message),
    warn: (message: string) => log("warn", message),
    error: (message: string) => log("error", message),
    critical: (message: string) => log("critical", message),
}

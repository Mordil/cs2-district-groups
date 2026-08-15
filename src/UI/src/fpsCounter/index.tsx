import { useValue } from "cs2/api"
import { useEffect, useRef, useState } from "react"
import { isDebugBuild$ } from "../shared"
import { debugShowFpsCounter$ } from "./bindings"
import { styles } from "./styles"

// Averages over a window rather than showing a single frame's instantaneous
// delta - a raw per-frame number is too jumpy to read at a glance.
const kSampleIntervalMs = 500

export const FpsCounter = () => {
    const isDebugBuild = useValue(isDebugBuild$)
    const showFpsCounter = useValue(debugShowFpsCounter$)
    const shouldRun = isDebugBuild && showFpsCounter
    const [fps, setFps] = useState(0)
    const frameCountRef = useRef(0)
    const sampleStartRef = useRef(0)

    useEffect(() => {
        if (!shouldRun) {
            return
        }

        let rafHandle = 0
        frameCountRef.current = 0
        sampleStartRef.current = performance.now()

        const tick = () => {
            frameCountRef.current += 1
            const now = performance.now()
            const elapsed = now - sampleStartRef.current
            if (elapsed >= kSampleIntervalMs) {
                setFps(Math.round((frameCountRef.current * 1000) / elapsed))
                frameCountRef.current = 0
                sampleStartRef.current = now
            }
            rafHandle = requestAnimationFrame(tick)
        }
        rafHandle = requestAnimationFrame(tick)

        return () => cancelAnimationFrame(rafHandle)
    }, [shouldRun])

    // Bail before the hook even matters for a Release build (isDebugBuild
    // always false there) - no rAF loop running, nothing rendered.
    if (!shouldRun) {
        return null
    }

    return <div style={styles.container}>{fps} FPS</div>
}

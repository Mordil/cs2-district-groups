import { useEffect, useRef, useState } from "react"

export type TransitionPhase = "enter" | "enterActive" | "exit" | "exitActive"

interface Options {
    // Set false to play the enter transition immediately on mount instead
    // of skipping it — for a component that's freshly created each time
    // it should be shown (e.g. a flyout mounted by its parent only while
    // open), rather than one that toggles visibility while staying mounted.
    skipInitial?: boolean
}

/*
    Drives the enter/enterActive/exit/exitActive class-swap pattern (see
    styles/transitions.scss) from a single boolean, and keeps a component
    mounted through its exit animation instead of unmounting it instantly.

    On `active` flipping true: mounts immediately at "enter" (from-state),
    then flips to "enterActive" (to-state) a couple of frames later so the
    browser actually paints the from-state first instead of coalescing both
    into one frame. Flipping false mirrors this into "exit"/"exitActive",
    then unmounts after `durationMs`. The initial mount skips straight to
    the steady phase (unless `skipInitial: false`), so nothing animates
    until `active` actually changes.
*/
export function useEnterExitPhase(active: boolean, durationMs: number, options: Options = {}) {
    const { skipInitial = true } = options
    const [phase, setPhase] = useState<TransitionPhase>(active ? "enterActive" : "exitActive")
    const [mounted, setMounted] = useState(active)
    const isInitialMount = useRef(true)

    useEffect(() => {
        if (isInitialMount.current) {
            isInitialMount.current = false
            if (skipInitial) {
                return
            }
        }

        setPhase(active ? "enter" : "exit")
        if (active) {
            setMounted(true)
        }

        let raf2 = 0
        const raf1 = requestAnimationFrame(() => {
            raf2 = requestAnimationFrame(() => {
                setPhase(active ? "enterActive" : "exitActive")
            })
        })

        return () => {
            cancelAnimationFrame(raf1)
            cancelAnimationFrame(raf2)
        }
    }, [active])

    useEffect(() => {
        if (active || phase !== "exitActive") {
            return
        }
        const timeout = window.setTimeout(() => setMounted(false), durationMs)
        return () => window.clearTimeout(timeout)
    }, [active, phase, durationMs])

    return { phase, mounted }
}

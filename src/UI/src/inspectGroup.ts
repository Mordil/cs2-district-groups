import { useEffect, useRef } from "react"

import { Entity } from "cs2/utils"

/*
    The district info panel's Inspect action has to reach the group manager panel, which the game
    mounts as its own separate React root - so there are no shared props or context between the two.
    A module-level listener set is the only channel they both have.
*/

type InspectListener = (group: Entity) => void

const kListeners = new Set<InspectListener>()

// Asks the group manager to open one group's details.
export const requestGroupInspection = (group: Entity) => {
    kListeners.forEach((listener) => listener(group))
}

// Answers inspection requests for as long as the calling component stays mounted.
export const useGroupInspectionRequests = (onRequest: InspectListener) => {
    const latest = useRef(onRequest)
    latest.current = onRequest

    useEffect(() => {
        const listener: InspectListener = (group) => latest.current(group)
        kListeners.add(listener)

        return () => {
            kListeners.delete(listener)
        }
    }, [])
}

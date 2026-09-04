import { Icon, Portal, Tooltip } from "cs2/ui"
import { Entity } from "cs2/utils"
import { ReactNode, useEffect, useRef, useState } from "react"
import { useTranslation } from "../../locale"
import { glyphIconSrc } from "../icons"
import { VT } from "../vanilla"
import css from "./index.module.scss"
import { GroupOption } from "./candidates"
import { AnchorEdges, GroupSearchFlyout } from "./GroupSearchFlyout"

export { eligibleGroups, groupCandidatesByType, kGenericGroupType } from "./candidates"
export type { GroupOption, GroupSection } from "./candidates"

// A flyout is a screen-level popup, so at most one may ever be on screen:
// opening one closes whichever selector was already showing its flyout.
let closeOpenFlyout: (() => void) | null = null

interface GroupSelectorProps {
    buildingType: number
    candidates: GroupOption[]
    hasAssignment: boolean
    assignedGroupName: string
    onSelect: (group: Entity) => void
    onUnassign: () => void
    tooltip?: ReactNode
    className?: string
}

export const GroupSelector = (props: GroupSelectorProps) => {
    const t = useTranslation()
    const toggleRef = useRef<HTMLButtonElement>(null)
    const flyoutRef = useRef<HTMLDivElement>(null)
    const [open, setOpen] = useState(false)
    const [anchorEdges, setAnchorEdges] = useState<AnchorEdges | null>(null)

    const measureAnchor = () => {
        if (toggleRef.current) {
            const { left, right } = toggleRef.current.getBoundingClientRect()
            setAnchorEdges({ left, right })
        }
    }

    const openFlyout = () => {
        closeOpenFlyout?.()

        measureAnchor()
        setOpen(true)

        closeOpenFlyout = () => setOpen(false)
    }

    const closeFlyout = () => {
        setOpen(false)

        closeOpenFlyout = null
    }

    useEffect(() => {
        if (!open) {
            return
        }

        const onPointerDown = (e: PointerEvent) => {
            const target = e.target as Node
            if (toggleRef.current?.contains(target)) {
                return
            }
            if (flyoutRef.current?.contains(target)) {
                return
            }
            closeFlyout()
        }

        const onKeyDown = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                closeFlyout()
            }
        }

        document.addEventListener("pointerdown", onPointerDown, true)
        document.addEventListener("keydown", onKeyDown)
        window.addEventListener("scroll", measureAnchor, true)
        window.addEventListener("resize", measureAnchor)

        return () => {
            document.removeEventListener("pointerdown", onPointerDown, true)
            document.removeEventListener("keydown", onKeyDown)
            window.removeEventListener("scroll", measureAnchor, true)
            window.removeEventListener("resize", measureAnchor)
        }
    }, [open])

    const onSelect = (group: Entity) => {
        props.onSelect(group)
        closeFlyout()
    }

    const onUnassign = () => {
        props.onUnassign()
        closeFlyout()
    }

    return (
        <>
            <Tooltip tooltip={props.tooltip}>
                <button
                    ref={toggleRef}
                    className={[
                        VT.sectionPrimaryButton.button,
                        css.toggle,
                        props.className ?? "",
                        open ? "selected" : "",
                    ].filter(Boolean).join(" ")}
                    onClick={() => (open ? closeFlyout() : openFlyout())}
                >
                    <div className={css.label}>
                        {props.hasAssignment ? props.assignedGroupName : t("unassignedLabel")}
                    </div>

                    <Icon
                        className={`${VT.sectionPrimaryButton.icon} ${css.chevron}`}
                        tinted={true}
                        src={glyphIconSrc("ThickStrokeArrowRight")}
                    />
                </button>
            </Tooltip>

            {open && anchorEdges && (
                <Portal>
                    <GroupSearchFlyout
                        rootRef={flyoutRef}
                        anchorEdges={anchorEdges}
                        buildingType={props.buildingType}
                        candidates={props.candidates}
                        hasAssignment={props.hasAssignment}
                        onSelect={onSelect}
                        onUnassign={onUnassign}
                        onClose={closeFlyout}
                    />
                </Portal>
            )}
        </>
    )
}

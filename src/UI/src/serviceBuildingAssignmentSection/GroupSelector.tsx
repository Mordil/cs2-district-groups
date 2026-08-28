import { Color } from "cs2/bindings"
import { trigger } from "cs2/api"
import { Icon, Portal } from "cs2/ui"
import { Entity } from "cs2/utils"
import { useEffect, useRef, useState } from "react"
import mod from "../../mod.json"
import { useTranslation } from "../locale"
import { logger } from "../log"
import { glyphIconSrc } from "../components/icons"
import { VT } from "../components/vanilla"
import css from "./GroupSelector.module.scss"
import { AnchorEdges, GroupSearchFlyout } from "./GroupSearchFlyout"

const kDisplayNameMaxCharacterCount = 24

export interface GroupOption {
    entity: Entity
    name: string
    type: number
    color: Color
}

export interface GroupSection {
    type: number
    options: GroupOption[]
}

// Indexed the same as useTypeLabels, order must match the C# GroupServiceType enum.
export const kGenericGroupType = 0

// One section per candidate type, each alphabetized - the building's own
// matching type (if not Generic) leads, remaining types follow in enum order.
// The building's own type and Generic always get a section, even if empty.
export const groupCandidatesByType = (candidates: GroupOption[], buildingType: number): GroupSection[] => {
    const byType = new Map<number, GroupOption[]>()
    byType.set(buildingType, [])
    byType.set(kGenericGroupType, [])
    for (const candidate of candidates) {
        const options = byType.get(candidate.type)
        if (options) {
            options.push(candidate)
        } else {
            byType.set(candidate.type, [candidate])
        }
    }

    const types = [...byType.keys()].sort((a, b) => {
        const aMatches = a === buildingType
        const bMatches = b === buildingType
        if (aMatches !== bMatches) {
            return aMatches ? -1 : 1
        }
        return a - b
    })

    return types.map((type) => ({
        type,
        options: [...byType.get(type)!].sort((a, b) => a.name.localeCompare(b.name)),
    }))
}

interface GroupSelectorProps {
    buildingType: number
    candidates: GroupOption[]
    hasAssignment: boolean
    assignedGroupName: string
}

// Toggle button + search flyout for assigning a district group to a service building
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
        measureAnchor()
        setOpen(true)
    }

    const closeFlyout = () => setOpen(false)

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

    const onSelect = (entity: Entity) => {
        logger.info(`Assign group clicked; entity:${entity.index}:${entity.version}`)
        trigger(mod.id, "assignGroup", entity)
        closeFlyout()
    }

    const onUnassign = () => {
        logger.info("Unassign group clicked;")
        trigger(mod.id, "unassignGroup")
        closeFlyout()
    }

    return (
        <>
            <button
                ref={toggleRef}
                className={[
                    VT.sectionPrimaryButton.button,
                    css.toggle,
                    open ? "selected" : "",
                ].filter(Boolean).join(" ")}
                style={{
                    maxWidth: '75%'
                }}
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

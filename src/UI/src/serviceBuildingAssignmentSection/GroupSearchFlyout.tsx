import { Color } from "cs2/bindings"
import { Button, Scrollable, Tooltip } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import { MouseEvent, RefObject, useState } from "react"
import { useTranslation } from "../locale"
import { useTypeLabels, kUITopOffset } from "../constants"
import { VC, VF, VT } from "../components/vanilla"
import css from "./GroupSearchFlyout.module.scss"
import { GroupOption, groupCandidatesByType } from "./GroupSelector"
import { useEnterExitPhase } from "../hooks/useEnterExitPhase"

const kFlyoutWidthPx = 580
const kGapPx = 24

// Matches the mixin's own transition duration in GroupSearchFlyout.module.scss.
const kFadeDurationMs = 120

// Color channels arrive as 0-1 floats (see ColorPicker.tsx's own {..., a: 1} clamp).
const colorToCss = (c: Color): string =>
    `rgba(${Math.round(c.r * 255)}, ${Math.round(c.g * 255)}, ${Math.round(c.b * 255)}, ${c.a})`

// Only the horizontal edges of the toggle button are ever needed (to decide which side of it the flyout opens on)
export interface AnchorEdges {
    left: number
    right: number
}

interface GroupSearchFlyoutProps {
    rootRef: RefObject<HTMLDivElement>
    anchorEdges: AnchorEdges
    buildingType: number
    candidates: GroupOption[]
    hasAssignment: boolean
    onSelect: (entity: Entity) => void
    onUnassign: () => void
    onClose: () => void
}

export const GroupSearchFlyout = (props: GroupSearchFlyoutProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [query, setQuery] = useState("")
    const [searchFocused, setSearchFocused] = useState(false)
    // This component is only ever mounted by its parent while it should be
    // shown (see GroupSelector's `open && anchorEdges` check), so it's
    // "active" for its whole lifetime — skipInitial: false makes the enter
    // transition play immediately on mount instead of being skipped.
    const { phase } = useEnterExitPhase(true, kFadeDurationMs, { skipInitial: false })

    const showPlaceholder = !searchFocused && query.length === 0

    const trimmedQuery = query.trim().toLowerCase()
    const sections = groupCandidatesByType(props.candidates, props.buildingType)
        .map((section) => ({
            ...section,
            options: trimmedQuery.length === 0
                ? section.options
                : section.options.filter((c) => c.name.toLowerCase().includes(trimmedQuery)),
        }))
        .filter((section) => section.options.length > 0)

    const spaceRight = window.innerWidth - props.anchorEdges.right
    const openToLeft = spaceRight < kFlyoutWidthPx + kGapPx
    const left = openToLeft
        ? props.anchorEdges.left - kFlyoutWidthPx - kGapPx
        : props.anchorEdges.right + kGapPx
    return (
        <div
            ref={props.rootRef}
            className={`${css.panel} ${css[phase]}`}
            style={{
                position: "fixed",
                top: `${kUITopOffset}rem`,
                left: `${left}px`,
                width: `calc(400rem*(.33333+ var(--fontScale) /1.5))`,
                maxWidth: `${kFlyoutWidthPx}px`,
                maxHeight: '80vh',
            }}
        >
            <div className={css.header}>
                <div className={css.titleSection}>
                    <span className={css.title}>{t("groupSearchTitle")}</span>
                    <VC.IconButton
                        tinted={true}
                        focusKey={VF.FOCUS_DISABLED}
                        src={VT.panel.closeIcon}
                        theme={VT.roundIconButton}
                        className={VT.panel.closeButton}
                        onSelect={props.onClose}
                        onMouseDown={(e: MouseEvent) => {
                            e.preventDefault()
                            e.stopPropagation()
                        }}
                    />
                </div>

                <div className={css.actionSection}>
                    <Tooltip
                        tooltip={props.hasAssignment
                            ? t("unassignTooltipEnabled")
                            : t("unassignTooltipDisabled")}
                    >
                        <Button
                            variant="flat"
                            disabled={!props.hasAssignment}
                            className={css.unassignButton}
                            onSelect={props.onUnassign}
                        >
                            {t("unassignOption")}
                        </Button>
                    </Tooltip>
                    <input
                        className={showPlaceholder
                            ? `${css.searchInput} ${css.searchInputPlaceholder}`
                            : css.searchInput}
                        value={showPlaceholder ? t("searchGroupsPlaceholder") : query}
                        onFocus={() => setSearchFocused(true)}
                        onBlur={() => setSearchFocused(false)}
                        onChange={(e) => setQuery((e.target as HTMLInputElement).value)}
                    />
                </div>
            </div>

            <Scrollable vertical={true} className={css.list}>
                {sections.length === 0 && trimmedQuery.length > 0 ? (
                    <div className={css.empty}>{t("noGroupsMatchSearch")}</div>
                ) : (
                    sections.map((section) => (
                        <div key={section.type}>
                            <div className={css.listSectionHeader}>{typeLabels[section.type] ?? "?"}</div>
                            {section.options.map((candidate) => (
                                <div
                                    key={entityKey(candidate.entity)}
                                    className={css.listItem}
                                    onClick={() => props.onSelect(candidate.entity)}
                                >
                                    <span
                                        className={css.colorSwatch}
                                        style={{ background: colorToCss(candidate.color) }}
                                    />
                                    <span className={css.listItemName}>{candidate.name}</span>
                                </div>
                            ))}
                        </div>
                    ))
                )}
            </Scrollable>
        </div>
    )
}

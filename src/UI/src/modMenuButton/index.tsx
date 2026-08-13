import { trigger, useValue } from "cs2/api"
import { FormattedParagraphs, Tooltip } from "cs2/ui"
import { useState } from "react"
import mod from "../../mod.json"
import { ModIcon } from "../components/icons"
import { markdownRenderer } from "../shared"
import { useTranslation } from "../locale"
import { groups$, GroupManagementPanel } from "groupManagementPanel"
import css from "./index.module.scss"

// Matches panelShellStyle's own transition duration below.
const kFadeDurationMs = 150

const panelShellStyle = {
    position: "absolute",
    top: "60rem",
    left: "10rem",
    width: "490rem",
    height: "80vh",
    display: "flex",
    flexDirection: "column",
    color: "white",
    borderRadius: "6rem",
    fontSize: "14rem",
    overflow: "hidden",
    transition: "opacity .15s ease",
} as const

/*

    The panel shell stays permanently mounted for the fade transition, but
    GroupManagementPanel (and the Dropdown/Tooltip it contains) is
    unmounted after the fade-out completes and freshly remounted on every
    open.

    This avoids Dropdown/Tooltip ever initializing their hover
    wiring while an ancestor is opacity:0/pointer-events:none — the state
    that broke the filter's tooltip when the content stayed mounted
    through the very first (hidden) render.
*/

export const GroupManager = () => {
    const t = useTranslation()
    const [open, setOpen] = useState(false)
    const [contentMounted, setContentMounted] = useState(false)
    const groups = useValue(groups$)

    const openPanel = () => {
        setOpen(true)
        trigger(mod.id, "setOverlay", true)
        setContentMounted(true)
    }

    const closePanel = () => {
        setOpen(false)
        trigger(mod.id, "setOverlay", false)
        window.setTimeout(() => setContentMounted(false), kFadeDurationMs)
    }

    const togglePanel = () => (open ? closePanel() : openPanel())

    // Built per-render (not a module-level constant like other tooltips)
    // since it needs the live group count.
    const panelToggleTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                t("toggleTooltipTitle"),
                t("toggleTooltipBody"),
                t("toggleTooltipCount", { count: groups.length }),
            ]}
        />
    )

    return (
        <>
            <Tooltip tooltip={panelToggleTooltip}>
                <button className={css.panelToggleButton} onClick={togglePanel}>
                    <ModIcon name="DistrictGroupRing" size="28rem" />
                </button>
            </Tooltip>
            <div
                style={{
                    ...panelShellStyle,
                    opacity: open ? 1 : 0,
                    pointerEvents: open ? "auto" : "none",
                }}
            >
                {contentMounted && <GroupManagementPanel />}
            </div>
        </>
    )
}

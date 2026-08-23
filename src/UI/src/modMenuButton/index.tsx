import { trigger, useValue } from "cs2/api"
import { Button, FormattedParagraphs, Tooltip } from "cs2/ui"
import { useEffect, useRef, useState } from "react"
import mod from "../../mod.json"
import { kIconStylePaths, kUITopOffset } from "../constants"
import { markdownRenderer } from "../shared"
import { useTranslation } from "../locale"
import { areaToolActive$, GroupManagementPanel } from "groupManagementPanel"
import { logger } from "../log"

// Matches panelShellStyle's own transition duration below.
const kFadeDurationMs = 150

const panelShellStyle = {
    position: "absolute",
    top: `${kUITopOffset}rem`,
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
    const areaToolActive = useValue(areaToolActive$)
    const iconPath = kIconStylePaths[open ? 0 : 1]
    const dismissedByAreaTool = useRef(false)

    const openPanel = () => {
        logger.info("Panel opened;")
        setOpen(true)
        trigger(mod.id, "setOverlay", true)
        setContentMounted(true)
    }

    const closePanel = () => {
        logger.info("Panel closed;")
        setOpen(false)
        trigger(mod.id, "setOverlay", false)
        window.setTimeout(() => setContentMounted(false), kFadeDurationMs)
    }

    const togglePanel = () => (open ? closePanel() : openPanel())

    // The district area tool shares the same screen space our panel occupies
    // when it comes up, we dismiss our UI
    // then afterwards, we show our UI
    useEffect(() => {
        if (areaToolActive) {
            if (open) {
                dismissedByAreaTool.current = true
                closePanel()
            }
        } else if (dismissedByAreaTool.current) {
            dismissedByAreaTool.current = false
            openPanel()
        }
    }, [areaToolActive])

    const panelToggleTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("toggleTooltipTitle"), t("toggleTooltipBody")]}
        />
    )

    return (
        <>
            <Tooltip tooltip={panelToggleTooltip}>
                <Button
                    variant="floating"
                    src={iconPath}
                    tinted={!open}
                    selected={open}
                    onSelect={togglePanel}
                />
            </Tooltip>
            <div
                style={{
                    ...panelShellStyle,
                    opacity: open ? 1 : 0,
                    pointerEvents: open ? "auto" : "none",
                }}
            >
                {contentMounted && <GroupManagementPanel onClose={closePanel} />}
            </div>
        </>
    )
}

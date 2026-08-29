import { trigger, useValue } from "cs2/api"
import { infoview } from "cs2/bindings"
import { Button, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityEquals } from "cs2/utils"
import { useEffect, useRef, useState } from "react"
import mod from "../../mod.json"
import { kIconStylePaths, kUITopOffset } from "../constants"
import { markdownRenderer } from "../shared"
import { useTranslation } from "../locale"
import { areaToolActive$, overlayVisible$, selectingGroup$, shouldDismissPanel$, GroupManagementPanel } from "groupManagementPanel"
import { logger } from "../log"
import { useEnterExitPhase } from "../hooks/useEnterExitPhase"
import css from "./index.module.scss"

// Matches the mixin's own transition duration in index.module.scss.
const kFadeDurationMs = 150

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
    const { phase, mounted: contentMounted } = useEnterExitPhase(open, kFadeDurationMs)
    const areaToolActive = useValue(areaToolActive$)
    const overlayVisible = useValue(overlayVisible$)
    const selectingGroup = useValue(selectingGroup$)
    const shouldDismissPanel = useValue(shouldDismissPanel$)
    const iconPath = kIconStylePaths[open ? 0 : 1]
    const dismissedByAreaTool = useRef(false)

    const openPanel = () => {
        logger.info("Panel opened;")
        setOpen(true)
        trigger(mod.id, "setOverlay", true)
        // We occupy the same corner as the Info Views menu - dismiss it so they don't overlap.
        infoview.closeInfoviewMenu()
    }

    const closePanel = () => {
        logger.info("Panel closed;")
        setOpen(false)
        trigger(mod.id, "setOverlay", false)

        // we don't want to leave the player in a weird tool state after dismissing our UI
        if (!entityEquals(selectingGroup, { index: 0, version: 0 })) {
            logger.info("Dismissing UI with active district selection, toggling off;")
            trigger(mod.id, "toggleDistrictSelection", selectingGroup)
        }
    }

    const togglePanel = () => (open ? closePanel() : openPanel())

    // The district area tool shares the same screen space our panel occupies.
    // If it opens while we're displaying our UI, we want to dismiss until the player is done.
    // Then restore our UI, because they may want to do something with the areas they just painted.
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

    // Every other reason our panel doesn't belong on screen right now
    // Unlike the area tool, this is likely not a temporary detour worth restoring our panel after.
    useEffect(() => {
        if (open && (shouldDismissPanel || !overlayVisible)) {
            closePanel()
        }
    }, [shouldDismissPanel, overlayVisible])

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

            <div className={`${css.panelShell} ${css[phase]}`}>
                {contentMounted &&
                    <GroupManagementPanel onClose={closePanel} />
                }
            </div>
        </>
    )
}

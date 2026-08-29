import { trigger, useValue } from "cs2/api"
import { game, infoview, selectedInfo } from "cs2/bindings"
import { Button, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityEquals } from "cs2/utils"
import { useEffect, useRef, useState } from "react"
import mod from "../../mod.json"
import { kIconStylePaths, kUITopOffset } from "../constants"
import { markdownRenderer } from "../shared"
import { useTranslation } from "../locale"
import { areaToolActive$, cameraModeActive$, otherToolActive$, overlayVisible$, selectingGroup$, GroupManagementPanel } from "groupManagementPanel"
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
    const selectedEntity = useValue(selectedInfo.selectedEntity$)
    const activeGamePanel = useValue(game.activeGamePanel$)
    const cameraModeActive = useValue(cameraModeActive$)
    const otherToolActive = useValue(otherToolActive$)
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

    // We occupy the same area as other game panels, so if they open, dismiss ourselves
    // Because these actions are entirely different concerns from users from what we do, don't reopen after they close
    useEffect(() => {
        if (!open) {
            return
        }
        const infoviewMenuOpen = activeGamePanel?.__Type === game.GamePanelType.InfoviewMenu
        const entitySelected = !entityEquals(selectedEntity, { index: 0, version: 0 })
        if (entitySelected || infoviewMenuOpen) {
            closePanel()
        }
    }, [selectedEntity, activeGamePanel])

    // Camera Mode, so dismiss and don't reopen after it ends
    useEffect(() => {
        if (open && cameraModeActive) {
            closePanel()
        }
    }, [cameraModeActive])

    // Some other vanilla tool came up with its own info panel in our screen corner - dismiss and don't reopen after it ends
    useEffect(() => {
        if (open && otherToolActive) {
            closePanel()
        }
    }, [otherToolActive])

    // If something happens code side that we need to close, respect it
    useEffect(() => {
        if (open && !overlayVisible) {
            closePanel()
        }
    }, [overlayVisible])

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

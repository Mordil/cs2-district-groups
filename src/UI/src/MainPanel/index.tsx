import { MouseEvent, useEffect, useRef, useState } from "react"

import { useValue } from "cs2/api"
import { AutoNavigationScope, InputActionConsumer, NavigationDirection } from "cs2/input"
import { Button, FormattedParagraphs, Tooltip } from "cs2/ui"

import { areasVisible$, showOverlay$, showServiceBuildings$ } from "../bindings"
import { Checkbox } from "../components/Checkbox"
import { glyphIconSrc, modIconSrc } from "../components/icons"
import { TypeFilterPicker } from "../components/TypePicker"
import { VC, VF, VT } from "../components/vanilla"
import { useTypeLabels } from "../constants"
import { markdownRenderer } from "../shared"
import {
    createGroup as createGroupTrigger,
    setAreasVisible,
    setOverlayFilter,
    setShowOverlay,
    setShowServiceBuildings,
} from "../triggers"
import { useTranslation } from "../utils/locale"
import { logger } from "../utils/log"

import { BuildingAssignmentsTab } from "./BuildingAssignmentsTab"
import { GroupManagementTab } from "./GroupManagementTab"
import css from "./index.module.scss"

enum PanelTab {
    Groups = 0,
    Assignments = 1,
}

const kTabOrder = [PanelTab.Groups, PanelTab.Assignments]

interface MainPanelProps {
    onClose: () => void
}

let lastFilterType = 0
let lastPanelTab = PanelTab.Groups
export const MainPanel = ({ onClose }: MainPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [filterType, setFilterType] = useState(lastFilterType)
    const [activeTab, setActiveTab] = useState(lastPanelTab)
    const [hideAssigned, setHideAssigned] = useState(false)
    const areasVisible = useValue(areasVisible$)
    const showOverlay = useValue(showOverlay$)
    const showServiceBuildings = useValue(showServiceBuildings$)

    // The Assignments tab is useless without the service-building markers on screen, so being on it forces them on.
    // True only while that override is ours to undo a manual toggle by the player replaces it, and never gets overridden back.
    const forcedShowServiceBuildings = useRef(false)

    const filterTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[activeTab === PanelTab.Groups ? t("filterTooltipLine1") : t("filterTooltipAssignmentsLine1")]}
            nonInline
        />
    )

    const hideAssignedTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("hideAssignedBuildingsTooltip")]}
        />
    )

    const onFilterChange = (type: number) => {
        logger.info(`Filter changed; type:${type}`)
        lastFilterType = type
        setFilterType(type)
        setOverlayFilter(type)
    }

    const onTabSelect = (tab: PanelTab) => {
        logger.info(`Panel tab changed; tab:${PanelTab[tab]}`)
        lastPanelTab = tab
        setActiveTab(tab)
    }

    // Puts back what the player had before the assignments tab forced the markers on.
    const clearForcedShowServiceBuildings = () => {
        if (!forcedShowServiceBuildings.current) {
            return
        }
        logger.info("Restoring service buildings off after leaving the assignments tab;")
        forcedShowServiceBuildings.current = false
        setShowServiceBuildings(false)
    }

    useEffect(() => {
        if (activeTab !== PanelTab.Assignments) {
            clearForcedShowServiceBuildings()
            return
        }

        setHideAssigned(false)

        if (!showServiceBuildings) {
            logger.info("Forcing service buildings on for the assignments tab;")
            forcedShowServiceBuildings.current = true
            setShowServiceBuildings(true)
        }
    }, [activeTab])

    // Closing the panel unmounts us, which counts as leaving the tab.
    useEffect(() => clearForcedShowServiceBuildings, [])

    const onCreateGroup = () => {
        logger.info("New group clicked;")
        createGroupTrigger()
    }

    const onHideAssignedChange = (checked: boolean) => {
        logger.info(`Hide assigned buildings toggled; hide:${checked}`)
        setHideAssigned(checked)
    }

    const onAreasVisibleChange = (checked: boolean) => {
        logger.info(`Areas visible toggled; visible:${checked}`)
        setAreasVisible(checked)
    }

    const onShowOverlayChange = (checked: boolean) => {
        logger.info(`Show group overlay toggled; show:${checked}`)
        setShowOverlay(checked)
    }

    const onShowServiceBuildingsChange = (checked: boolean) => {
        logger.info(`Show service buildings toggled; show:${checked}`)
        // The player's own choice outlives the assignments tab's override.
        forcedShowServiceBuildings.current = false
        setShowServiceBuildings(checked)
    }

    return (
        <InputActionConsumer actions={{ Close: onClose, Back: onClose }} ignoreFocusState={true}>
            <div className={css.panel}>
                <div className={css.header}>
                    <div className={css.titleRow}>
                        <span className={css.title}>{t("panelTitle")}</span>
                        <VC.IconButton
                            tinted={true}
                            focusKey={VF.FOCUS_DISABLED}
                            src={VT.panel.closeIcon}
                            theme={VT.roundIconButton}
                            className={VT.panel.closeButton}
                            onSelect={onClose}
                            onMouseDown={(e: MouseEvent) => {
                                e.preventDefault()
                                e.stopPropagation()
                            }}
                        />
                    </div>

                    <VC.TabBar className={css.tabBar}>
                        <Tooltip tooltip={t("groupsTabLabel")}>
                            <VC.Tab
                                id={PanelTab.Groups}
                                selectedId={activeTab}
                                onSelect={onTabSelect}
                            >
                                <VC.TintedIcon
                                    src={glyphIconSrc("Info")}
                                    className={css.tabIcon}
                                />
                            </VC.Tab>
                        </Tooltip>

                        <Tooltip tooltip={t("assignmentsTabLabel")}>
                            <VC.Tab
                                id={PanelTab.Assignments}
                                selectedId={activeTab}
                                onSelect={onTabSelect}
                            >
                                <VC.TintedIcon
                                    src={modIconSrc("service-buildings")}
                                    className={css.tabIcon}
                                />
                            </VC.Tab>
                        </Tooltip>
                    </VC.TabBar>
                </div>

                <div className={css.panelContent}>
                    <div className={css.actionSection}>
                        <TypeFilterPicker
                            value={filterType}
                            onChange={onFilterChange}
                            labels={typeLabels}
                            allLabel={null}
                            tooltip={filterTooltip}
                        />

                        {activeTab === PanelTab.Groups && (
                            <Tooltip tooltip={t("newGroupButtonTooltip")}>
                                <Button
                                    variant="primary"
                                    className={css.newGroupButton}
                                    onSelect={onCreateGroup}
                                >
                                    {t("newGroupButton")}
                                </Button>
                            </Tooltip>
                        )}

                        {activeTab === PanelTab.Assignments && (
                            <Checkbox
                                checked={hideAssigned}
                                onChange={onHideAssignedChange}
                                label={t("hideAssignedBuildingsLabel")}
                                tooltip={hideAssignedTooltip}
                                className={css.hideAssignedToggle}
                            />
                        )}
                    </div>

                    <VC.TabNav
                        tabs={kTabOrder}
                        selectedTab={activeTab}
                        onSelect={onTabSelect}
                    >
                        <AutoNavigationScope
                            direction={NavigationDirection.Vertical}
                            allowLooping={true}
                        >
                            {activeTab === PanelTab.Groups ? (
                                <GroupManagementTab
                                    filterType={filterType}
                                    className={css.list}
                                />
                            ) : (
                                <BuildingAssignmentsTab
                                    filterType={filterType}
                                    hideAssigned={hideAssigned}
                                    className={css.list}
                                />
                            )}
                        </AutoNavigationScope>
                    </VC.TabNav>
                </div>

                <div className={css.footer}>
                    <Checkbox
                        checked={showOverlay}
                        onChange={onShowOverlayChange}
                        label={t("showGroupOverlayLabel")}
                        className={css.areasToggleRow}
                    />

                    <Checkbox
                        checked={showServiceBuildings}
                        onChange={onShowServiceBuildingsChange}
                        label={t("showServiceBuildingsLabel")}
                        className={css.areasToggleRow}
                    />

                    <Checkbox
                        checked={areasVisible}
                        onChange={onAreasVisibleChange}
                        label={t("displayDistrictAreasLabel")}
                        className={css.areasToggleRow}
                    />
                </div>
            </div>
        </InputActionConsumer>
    )
}

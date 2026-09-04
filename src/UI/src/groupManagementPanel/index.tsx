import { trigger, useValue } from "cs2/api"
import { AutoNavigationScope, InputActionConsumer, NavigationDirection } from "cs2/input"
import { Button, FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { MouseEvent, useEffect, useRef, useState } from "react"
import mod from "../../mod.json"
import { Checkbox } from "../components/Checkbox"
import { glyphIconSrc, modIconSrc } from "../components/icons"
import { TypeFilterPicker } from "../components/TypePicker"
import { VC, VF, VT } from "../components/vanilla"
import { useTypeLabels } from "../constants"
import { useTranslation } from "../locale"
import css from "./index.module.scss"
import { areasVisible$, groups$, showServiceBuildings$, showOverlay$ } from "./bindings"
import { markdownRenderer } from "../shared"
import { logger } from "../log"
import { GroupCard } from "./GroupCard"
import { AssignmentsTab } from "./AssignmentsTab"

export { groups$, areaToolActive$, shouldDismissPanel$, overlayVisible$, selectingGroup$ } from "./bindings"

enum PanelTab {
    Groups = 0,
    Assignments = 1,
}

const kTabOrder = [PanelTab.Groups, PanelTab.Assignments]

interface GroupManagementPanelProps {
    onClose: () => void
}

let lastFilterType = 0
let lastPanelTab = PanelTab.Groups
export const GroupManagementPanel = ({ onClose }: GroupManagementPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [filterType, setFilterType] = useState(lastFilterType)
    const [activeTab, setActiveTab] = useState(lastPanelTab)
    const [hideAssigned, setHideAssigned] = useState(false)
    const groups = useValue(groups$)
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

    // Groups matching the filtered type, in creation order (the binding's own order).
    const displayedGroups = groups.filter((g) => g.type === filterType)

    const onFilterChange = (type: number) => {
        logger.info(`Filter changed; type:${type}`)
        lastFilterType = type
        setFilterType(type)
        trigger(mod.id, "setOverlayFilter", type)
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
        trigger(mod.id, "setShowServiceBuildings", false)
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
            trigger(mod.id, "setShowServiceBuildings", true)
        }
    }, [activeTab])

    // Closing the panel unmounts us, which counts as leaving the tab.
    useEffect(() => clearForcedShowServiceBuildings, [])

    const onCreateGroup = () => {
        logger.info("New group clicked;")
        // groups.length (not displayedGroups.length) so the suggested name
        // reflects every group, regardless of the active filter.
        trigger(mod.id, "createGroup", t("newGroupDefaultName", { number: groups.length + 1 }), filterType)
    }

    const onHideAssignedChange = (checked: boolean) => {
        logger.info(`Hide assigned buildings toggled; hide:${checked}`)
        setHideAssigned(checked)
    }

    const onAreasVisibleChange = (checked: boolean) => {
        logger.info(`Areas visible toggled; visible:${checked}`)
        trigger(mod.id, "setAreasVisible", checked)
    }

    const onShowOverlayChange = (checked: boolean) => {
        logger.info(`Show group overlay toggled; show:${checked}`)
        trigger(mod.id, "setShowOverlay", checked)
    }

    const onShowServiceBuildingsChange = (checked: boolean) => {
        logger.info(`Show service buildings toggled; show:${checked}`)
        // The player's own choice outlives the assignments tab's override.
        forcedShowServiceBuildings.current = false
        trigger(mod.id, "setShowServiceBuildings", checked)
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
                                <Scrollable
                                    vertical={true}
                                    trackVisibility="reserve"
                                    className={css.list}
                                >
                                    {groups.length === 0 && (
                                        <div>{t("noGroupsYet")}</div>
                                    )}
                                    {groups.length > 0 && displayedGroups.length === 0 && (
                                        <div>{t("noGroupsMatchFilter")}</div>
                                    )}
                                    {displayedGroups.map((group) => (
                                        <GroupCard
                                            key={entityKey(group.entity)}
                                            group={group}
                                        />
                                    ))}
                                </Scrollable>
                            ) : (
                                <AssignmentsTab
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

import { trigger, useValue } from "cs2/api"
import { InputActionConsumer } from "cs2/input"
import { Button, FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { MouseEvent, useState } from "react"
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

// temporarily persisted value between UI mounting; 0 = Generic
let lastFilterType = 0

enum PanelTab {
    Groups = 0,
    Assignments = 1,
}

const kTabOrder = [PanelTab.Groups, PanelTab.Assignments]

interface GroupManagementPanelProps {
    onClose: () => void
}

export const GroupManagementPanel = ({ onClose }: GroupManagementPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [filterType, setFilterType] = useState(lastFilterType)
    const [activeTab, setActiveTab] = useState(PanelTab.Groups)
    const groups = useValue(groups$)
    const areasVisible = useValue(areasVisible$)
    const showOverlay = useValue(showOverlay$)
    const showServiceBuildings = useValue(showServiceBuildings$)

    const filterTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("filterTooltipLine1")]}
            nonInline
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
        setActiveTab(tab)
    }

    const onCreateGroup = () => {
        logger.info("New group clicked;")
        // groups.length (not displayedGroups.length) so the suggested name
        // reflects every group, regardless of the active filter.
        trigger(mod.id, "createGroup", t("newGroupDefaultName", { number: groups.length + 1 }), filterType)
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
                    </div>

                    <VC.TabNav
                        tabs={kTabOrder}
                        selectedTab={activeTab}
                        onSelect={onTabSelect}
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
                                className={css.list}
                            />
                        )}
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

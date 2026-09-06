import { CSSProperties, MouseEvent, useContext, useEffect, useState } from "react"

import { useValue } from "cs2/api"
import { InputActionConsumer } from "cs2/input"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityEquals, entityKey } from "cs2/utils"

import { selectingGroup$ } from "../bindings"
import { ColorPicker } from "../components/ColorPicker"
import { GroupTypeSelector } from "../components/GroupTypeSelector"
import { glyphIconSrc } from "../components/icons"
import { SelectDistrictsButton } from "../components/SelectDistrictsButton"
import { VC, VF, VT } from "../components/vanilla"
import { kGroupInfoPanelMaxWidth, kPanelWidth, useTypeLabels } from "../constants"
import { markdownRenderer } from "../shared"
import { deleteGroup, renameGroup, setGroupColor, setGroupType, toggleDistrictSelection } from "../triggers"
import { Group } from "../types"
import { useTranslation } from "../utils/locale"
import { logger } from "../utils/log"
import { TransitionPhase } from "../utils/useEnterExitPhase"

import { OverviewTab } from "./OverviewTab"
import css from "./index.module.scss"

// Tints the header delete action to flag it as the harder-to-reverse one.
const dangerIconStyle = { "--iconColor": "var(--negativeColor)" } as CSSProperties
const removeButtonStyle = { "height": "24rem", "width": "24rem" } as CSSProperties
const panelWidthStyle = { "minWidth": `${kPanelWidth}rem`, "maxWidth": `${kGroupInfoPanelMaxWidth}rem` } as CSSProperties

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

enum GroupInfoTab {
    Overview = 0,
}

const kTabOrder = [GroupInfoTab.Overview]

interface GroupInfoPanelProps {
    group: Group
    onClose: () => void
    phase: TransitionPhase
}

// Detail view for a single district group; the name and identity color are editable here.
let lastGroupInfoTab = GroupInfoTab.Overview
export const GroupInfoPanel = ({ group, onClose, phase }: GroupInfoPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const selectingGroup = useValue(selectingGroup$)
    const selectingDistricts = entityEquals(selectingGroup, group.entity)
    const [nameDraft, setNameDraft] = useState(group.name)
    const [nameFocused, setNameFocused] = useState(false)
    const [activeTab, setActiveTab] = useState(lastGroupInfoTab)
    const dialogStack = useContext(DialogStack)

    const deleteGroupTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("deleteGroupTooltipLine1"), t("deleteGroupTooltipLine2")]}
        />
    )

    const typePickerTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                t("typePickerTooltipLine1"),
                t("typePickerTooltipLine2"),
                t("typePickerTooltipLine3"),
            ]}
        />
    )

    /*
        Deleting an unassigned group is pretty easy to recover from; deleting one
        that's actively managing a building's operating districts is not, so let's get the user to double confirm.
    */
    const handleDeleteGroup = () => {
        logger.info(`Delete group clicked; entity:${entityKey(group.entity)}`)
        if (group.assignedBuildingCount === 0) {
            deleteGroup(group.entity)
            return
        }
        const deleteGroupMessage = t("deleteGroupConfirmMessage", {
            name: group.name,
            count: group.assignedBuildingCount,
        })
        dialogStack.showDialog(
            <ConfirmationDialog
                title={t("deleteGroupDialogTitle")}
                message={deleteGroupMessage}
                multiline={true}
                confirm={t("deleteGroupConfirm")}
                cancel={t("deleteGroupCancel")}
                onConfirm={() => {
                    logger.info(`Delete group confirmed; entity:${entityKey(group.entity)}`)
                    deleteGroup(group.entity)
                    dialogStack.closeAll()
                }}
                onCancel={() => {
                    logger.info(`Delete group dialog dismissed; entity:${entityKey(group.entity)}`)
                    dialogStack.closeAll()
                }}
            />
        )
    }

    /*
        Stay in sync with external changes (e.g. our own rename echoing back through the binding)
        but never while the user is actively typing, or every binding refresh would clobber in-progress edits.
    */
    useEffect(() => {
        if (!nameFocused) {
            setNameDraft(group.name)
        }
    }, [group.name, nameFocused])

    const commitName = () => {
        setNameFocused(false)
        const trimmed = nameDraft.trim()
        if (trimmed.length === 0) {
            setNameDraft(group.name)
        } else if (trimmed !== group.name) {
            logger.info(`Group renamed; entity:${entityKey(group.entity)} name:${trimmed}`)
            renameGroup(group.entity, trimmed)
        }
    }

    const onTabSelect = (tab: GroupInfoTab) => {
        logger.info(`Group info tab changed; tab:${GroupInfoTab[tab]}`)
        lastGroupInfoTab = tab
        setActiveTab(tab)
    }

    const handleClose = () => {
        if (selectingDistricts) {
            logger.info(`Closing group info panel with active district selection, toggling off; entity:${entityKey(group.entity)}`)
            toggleDistrictSelection(group.entity)
        }
        onClose()
    }

    return (
        <InputActionConsumer actions={{ Close: handleClose, Back: handleClose }} ignoreFocusState={true}>
            <div className={`${css.panel} ${css[phase]}`} style={panelWidthStyle}>
                <div className={css.header}>
                    <div className={css.titleRow}>
                        <ColorPicker
                            value={group.color}
                            onChange={(color) => {
                                logger.info(`Group color changed; entity:${entityKey(group.entity)}`)
                                setGroupColor(group.entity, color)
                            }}
                            tooltip={t("groupColorTooltip")}
                            className={css.colorSwatch}
                        />

                        <Tooltip tooltip={t("nameInputTooltip")}>
                            <input
                                className={css.title}
                                value={nameDraft}
                                onFocus={() => setNameFocused(true)}
                                onChange={(e) => setNameDraft((e.target as HTMLInputElement).value)}
                                onBlur={commitName}
                                onKeyDown={(e) => {
                                    if (e.key === "Enter") {
                                        (e.target as HTMLInputElement).blur()
                                    }
                                }}
                            />
                        </Tooltip>

                        <Tooltip tooltip={deleteGroupTooltip}>
                            <div className={css.deleteButtonHover}>
                                <VC.IconButton
                                    tinted={true}
                                    focusKey={VF.FOCUS_DISABLED}
                                    src={glyphIconSrc("Trash")}
                                    className={VT.districtsSection.deleteButton}
                                    style={{ ...dangerIconStyle, ...removeButtonStyle }}
                                    onSelect={handleDeleteGroup}
                                    onMouseDown={stopMouseDown}
                                />
                            </div>
                        </Tooltip>

                        <VC.IconButton
                            tinted={true}
                            focusKey={VF.FOCUS_DISABLED}
                            src={VT.panel.closeIcon}
                            theme={VT.roundIconButton}
                            className={VT.panel.closeButton}
                            onSelect={handleClose}
                            onMouseDown={stopMouseDown}
                        />
                    </div>
                </div>

                <div className={css.content}>
                    <div className={css.actionSection}>
                        <GroupTypeSelector
                            className={css.typeSelector}
                            value={group.type}
                            onChange={(newType) => {
                                logger.info(`Group type changed; entity:${entityKey(group.entity)} type:${newType}`)
                                setGroupType(group.entity, newType)
                            }}
                            labels={typeLabels}
                            icon="tag"
                            tooltip={typePickerTooltip}
                        />

                        <SelectDistrictsButton
                            selected={selectingDistricts}
                            className={css.selectDistrictButton}
                            onSelect={() => {
                                logger.info(`Toggle district selection clicked; entity:${entityKey(group.entity)}`)
                                toggleDistrictSelection(group.entity)
                            }}
                        />
                    </div>

                    <VC.TabBar className={css.tabBar}>
                        <VC.Tab
                            id={GroupInfoTab.Overview}
                            selectedId={activeTab}
                            className={css.tab}
                            onSelect={onTabSelect}
                        >
                            {t("overviewTabLabel")}
                        </VC.Tab>
                    </VC.TabBar>

                    <VC.TabNav
                        tabs={kTabOrder}
                        selectedTab={activeTab}
                        onSelect={onTabSelect}
                    >
                        <OverviewTab group={group} className={css.tabContent} />
                    </VC.TabNav>
                </div>
            </div>
        </InputActionConsumer>
    )
}

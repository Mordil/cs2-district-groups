import { CSSProperties, MouseEvent, useContext, useEffect, useState } from "react"

import { useValue } from "cs2/api"
import { camera } from "cs2/bindings"
import { InputActionConsumer } from "cs2/input"
import { LocalizedString } from "cs2/l10n"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { Entity, entityEquals, entityKey } from "cs2/utils"

import { serviceBuildings$ } from "../bindings"
import { ColorPicker } from "../components/ColorPicker"
import { glyphIconSrc } from "../components/icons"
import { TypePicker } from "../components/TypePicker"
import { VC, VF, VT } from "../components/vanilla"
import { kPanelWidth, useTypeLabels } from "../constants"
import { markdownRenderer } from "../shared"
import { deleteGroup, renameGroup, setGroupColor, setGroupType } from "../triggers"
import { Group } from "../types"
import { VanillaLocale, useTranslation } from "../utils/locale"
import { logger } from "../utils/log"
import { useEnterExitPhase } from "../utils/useEnterExitPhase"

import css from "./index.module.scss"

const kFadeDurationMs = 150

// Tints the header delete action to flag it as the harder-to-reverse one.
const dangerIconStyle = { "--iconColor": "var(--negativeColor)" } as CSSProperties
const removeButtonStyle = { "height": "24rem", "width": "24rem" } as CSSProperties

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

const focusTooltip = (
    <LocalizedString id={VanillaLocale.focusTooltip.id} fallback={VanillaLocale.focusTooltip.fallback} />
)

interface EntityRowProps {
    entity: Entity
    name: string
}

const EntityRow = ({ entity, name }: EntityRowProps) => (
    <div className={css.listItem}>
        <span className={css.listItemName}>{name}</span>
    </div>
)

interface GroupInfoPanelProps {
    group: Group
    onClose: () => void
}

// Detail view for a single district group; the name and identity color are editable here.
export const GroupInfoPanel = ({ group, onClose }: GroupInfoPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const serviceBuildings = useValue(serviceBuildings$)
    const { phase } = useEnterExitPhase(true, kFadeDurationMs, { skipInitial: false })
    const [nameDraft, setNameDraft] = useState(group.name)
    const [nameFocused, setNameFocused] = useState(false)
    const dialogStack = useContext(DialogStack)

    const assignedBuildings = serviceBuildings.filter((b) => entityEquals(b.assignedGroup, group.entity))

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

    return (
        <InputActionConsumer actions={{ Close: onClose, Back: onClose }} ignoreFocusState={true}>
            <div className={`${css.panel} ${css[phase]}`} style={{ left: `${kPanelWidth + 16}rem`, width: `${kPanelWidth}rem` }}>
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
                            onSelect={onClose}
                            onMouseDown={stopMouseDown}
                        />
                    </div>
                </div>

                <div className={css.content}>
                    <div className={css.actionSection}>
                        <TypePicker
                            value={group.type}
                            onChange={(newType) => {
                                logger.info(`Group type changed; entity:${entityKey(group.entity)} type:${newType}`)
                                setGroupType(group.entity, newType)
                            }}
                            labels={typeLabels}
                            tooltip={typePickerTooltip}
                        />
                    </div>

                    <Scrollable vertical={true} trackVisibility="reserve" className={css.scrollableContent}>
                        <div className={css.listSectionHeader}>{t("metadataDistrictsTooltip")}</div>

                        {group.members.map((member) => (
                            <EntityRow key={entityKey(member.entity)} entity={member.entity} name={member.name} />
                        ))}

                        <div className={css.listSectionHeader}>{t("metadataBuildingsTooltip")}</div>

                        {assignedBuildings.map((building) => (
                            <EntityRow key={entityKey(building.entity)} entity={building.entity} name={building.name} />
                        ))}
                    </Scrollable>
                </div>
            </div>
        </InputActionConsumer>
    )
}

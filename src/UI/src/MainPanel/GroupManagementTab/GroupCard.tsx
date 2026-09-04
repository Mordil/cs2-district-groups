import { CSSProperties, MouseEvent, useContext, useEffect, useState } from "react"

import { LocalizedNumber, Unit } from "cs2/l10n"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Icon, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { ColorPicker } from "../../components/ColorPicker"
import { gameIconSrc, glyphIconSrc, modIconSrc } from "../../components/icons"
import { TypePicker } from "../../components/TypePicker"
import { VC, VF, VT } from "../../components/vanilla"
import { useTypeLabels } from "../../constants"
import { markdownRenderer } from "../../shared"
import {
    deleteGroup,
    removeMember,
    renameGroup,
    setGroupColor,
    setGroupType,
    toggleDistrictSelection,
} from "../../triggers"
import { Group } from "../../types"
import { useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"
import { useEnterExitPhase } from "../../utils/useEnterExitPhase"

import css from "./GroupCard.module.scss"

// Matches the mixin's own transition duration in GroupCard.module.scss.
const kExpandDurationMs = 250

// Tints the trash icon on the group-level delete action to flag it as the
// harder-to-reverse one; the per-member remove below stays neutral.
const dangerIconStyle = { "--iconColor": "var(--negativeColor)" } as CSSProperties
const removeButtonStyle = { "height": "24rem", "width": "24rem" } as CSSProperties

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

const MetadataItem = (props: { icon: string; value: number; tooltip: string }) => (
    <Tooltip tooltip={props.tooltip}>
        <div className={css.metadataItem}>
            <Icon
                tinted={true}
                className={css.metadataIcon}
                src={props.icon} />

            <LocalizedNumber value={props.value} unit={Unit.Integer} />
        </div>
    </Tooltip>
)

interface GroupCardProps {
    group: Group
    selectingDistricts: boolean
}

export const GroupCard = ({ group, selectingDistricts }: GroupCardProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [expanded, setExpanded] = useState(false)
    const { phase: expandPhase, mounted: expandedContentMounted } = useEnterExitPhase(
        expanded,
        kExpandDurationMs
    )
    const [nameDraft, setNameDraft] = useState(group.name)
    const [nameFocused, setNameFocused] = useState(false)
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

    // Deleting an unassigned group is pretty easy to recover from; deleting one
    // that's actively managing a building's operating districts is not, so let's get the user to double confirm
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

    // Stay in sync with external changes (e.g. our own rename echoing back
    // through the binding) — but never while the user is actively typing,
    // or every binding refresh would clobber in-progress edits.
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

    const toggleExpanded = () => {
        const next = !expanded

        if (!next && selectingDistricts) {
            logger.info(`Collapsing group card with active district selection, toggling off; entity:${entityKey(group.entity)}`)
            toggleDistrictSelection(group.entity)
        }
        setExpanded(next)
    }

    return (
        <div className={css.groupCard}>
            <div className={`${css.groupDetailRow} ${(expanded ? '' : css.collapsed)}`}>
                <VC.IconButton
                    tinted={true}
                    focusKey={VF.FOCUS_DISABLED}
                    src={glyphIconSrc(expanded ? "ThickStrokeArrowDown" : "ThickStrokeArrowRight")}
                    theme={VT.roundIconButton}
                    className={css.rowIconButton}
                    onSelect={toggleExpanded}
                    onMouseDown={stopMouseDown}
                />
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
                        className={css.nameInput}
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

                <TypePicker
                    value={group.type}
                    onChange={(newType) => {
                        logger.info(`Group type changed; entity:${entityKey(group.entity)} type:${newType}`)
                        setGroupType(group.entity, newType)
                    }}
                    labels={typeLabels}
                    tooltip={typePickerTooltip}
                    style={{ marginRight: "4rem", maxWidth: "35%" }}
                />

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
            </div>

            <div className={css.metadataRow}>
                <MetadataItem
                    icon={gameIconSrc("LotTool")}
                    value={group.members.length}
                    tooltip={t("metadataDistrictsTooltip")}
                />
                <MetadataItem
                    icon={modIconSrc("building")}
                    value={group.assignedBuildingCount}
                    tooltip={t("metadataBuildingsTooltip")}
                />
                <MetadataItem
                    icon={gameIconSrc("Population")}
                    value={group.population}
                    tooltip={t("metadataPopulationTooltip")}
                />
            </div>

            {expandedContentMounted && (
                <div className={`${css.expandableContent} ${css[expandPhase]}`}>
                    <div className={css.memberList}>
                        {group.members.map((member) => (
                            <div className={css.memberRow} key={entityKey(member.entity)}>
                                <div className={css.memberName}>{member.name}</div>

                                <Tooltip tooltip={t("removeMemberTooltip")}>
                                    <div className={css.deleteButtonHover}>
                                        <VC.IconButton
                                            tinted={true}
                                            focusKey={VF.FOCUS_DISABLED}
                                            src={glyphIconSrc("Trash")}
                                            className={`${VT.districtsSection.deleteButton} ${css.memberDeleteButton}`}
                                            style={removeButtonStyle}
                                            onSelect={() => {
                                                logger.info(`Remove member clicked; entity:${entityKey(group.entity)} member:${entityKey(member.entity)}`)
                                                removeMember(group.entity, member.entity)
                                            }}
                                            onMouseDown={stopMouseDown}
                                        />
                                    </div>
                                </Tooltip>
                            </div>
                        ))}
                    </div>

                    <button
                        className={[VT.sectionPrimaryButton.button, css.selectDistrictsButton, selectingDistricts ? "selected" : ""]
                            .filter(Boolean).join(" ")}
                        onClick={() => {
                            logger.info(`Toggle district selection clicked; entity:${entityKey(group.entity)}`)
                            toggleDistrictSelection(group.entity)
                        }}
                    >
                        <Icon className={VT.sectionPrimaryButton.icon} src={gameIconSrc("Districts")} />
                        <span className={VT.sectionPrimaryButton.label}>{t("selectDistrictsButton")}</span>
                    </button>
                </div>
            )}
        </div>
    )
}

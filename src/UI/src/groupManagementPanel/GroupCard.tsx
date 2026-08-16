import { trigger, useValue } from "cs2/api"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityEquals, entityKey } from "cs2/utils"
import { useContext, useEffect, useState } from "react"
import mod from "../../mod.json"
import { ColorPicker } from "../components/ColorPicker"
import { UilIcon, GameIcon } from "../components/icons"
import { TypePicker } from "../components/TypePicker"
import css from "./index.module.scss"
import { Group } from "../types"
import { styles } from "./styles"
import { useTypeLabels } from "../constants"
import { useTranslation } from "../locale"
import { selectingGroup$ } from "./bindings"
import { markdownRenderer } from "../shared"
import { logger } from "../log"

export const GroupCard = (props: { group: Group }) => {
    const { group } = props
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [expanded, setExpanded] = useState(false)
    const [nameDraft, setNameDraft] = useState(group.name)
    const [nameFocused, setNameFocused] = useState(false)
    const selectingGroup = useValue(selectingGroup$)
    const selectingThisGroup = entityEquals(selectingGroup, group.entity)
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
            trigger(mod.id, "deleteGroup", group.entity)
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
                    trigger(mod.id, "deleteGroup", group.entity)
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
            trigger(mod.id, "renameGroup", group.entity, trimmed)
        }
    }

    const toggleExpanded = () => {
        logger.info(`Group card toggled; entity:${entityKey(group.entity)} expanded:${!expanded}`)
        setExpanded(!expanded)
    }

    return (
        <div style={styles.groupCard}>
            <div style={styles.row}>
                <button className={css.expandButton} onClick={toggleExpanded}>
                    <UilIcon name={expanded ? "ArrowDownThickStroke" : "ArrowRightThickStroke"} size="12rem" />
                </button>
                <ColorPicker
                    value={group.color}
                    onChange={(color) => {
                        logger.info(`Group color changed; entity:${entityKey(group.entity)}`)
                        trigger(mod.id, "setGroupColor", group.entity, color)
                    }}
                    tooltip={t("groupColorTooltip")}
                    className={css.colorSwatch}
                />
                <input
                    className={css.nameInput}
                    style={{ marginRight: "4rem" }}
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
                <TypePicker
                    value={group.type}
                    onChange={(newType) => {
                        logger.info(`Group type changed; entity:${entityKey(group.entity)} type:${newType}`)
                        trigger(mod.id, "setGroupType", group.entity, newType)
                    }}
                    labels={typeLabels}
                    tooltip={typePickerTooltip}
                    style={{ marginRight: "4rem" }}
                />
                <Tooltip tooltip={deleteGroupTooltip}>
                    <button
                        className={`${css.headerDeleteButton} ${css.dangerButton}`}
                        style={styles.dangerButton}
                        onClick={handleDeleteGroup}
                    >
                        <UilIcon name="Trash" size="20rem"/>
                    </button>
                </Tooltip>
            </div>

            {expanded && (
                <>
                    <div style={styles.memberList}>
                        {group.members.map((member) => (
                            <div style={styles.row} key={entityKey(member.entity)}>
                                <div style={{ flex: 1, paddingLeft: "40rem" }}>{member.name}</div>
                                <Tooltip tooltip={t("removeMemberTooltip")}>
                                    <button
                                        className={css.memberDeleteButton}
                                        onClick={() => {
                                            logger.info(`Remove member clicked; entity:${entityKey(group.entity)} member:${entityKey(member.entity)}`)
                                            trigger(mod.id, "removeMember", group.entity, member.entity)
                                        }}
                                    >
                                        <UilIcon name="Trash" size="20rem" />
                                    </button>
                                </Tooltip>
                            </div>
                        ))}
                    </div>

                    <button
                        className={`${css.selectDistrictsButton} ${selectingThisGroup ? css.selectDistrictsButtonActive : ""}`}
                        onClick={() => {
                            logger.info(`Toggle district selection clicked; entity:${entityKey(group.entity)}`)
                            trigger(mod.id, "toggleDistrictSelection", group.entity)
                        }}
                    >
                        <GameIcon name="Districts" size="16rem" />
                        <span style={{ marginLeft: "6rem" }}>{t("selectDistrictsButton")}</span>
                    </button>
                </>
            )}
        </div>
    )
}

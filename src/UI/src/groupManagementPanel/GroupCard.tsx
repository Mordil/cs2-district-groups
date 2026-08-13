import { trigger, useValue } from "cs2/api"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityEquals, entityKey } from "cs2/utils"
import { useContext, useEffect, useState } from "react"
import mod from "../../mod.json"
import { UilIcon, GameIcon } from "../components/icons"
import { TypePicker } from "../components/TypePicker"
import css from "./index.module.scss"
import { Group } from "../types"
import { styles } from "./styles"
import { kTypeLabels } from "../constants"
import { selectingGroup$ } from "./bindings"
import { markdownRenderer } from "../shared"

const deleteGroupTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Permanently delete the group.",
            "Assigned buildings will lose their **operating districts**."
        ]}
    />
)

const typePickerTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Change the **type** of the group.",
            "**Generic** groups can be assigned to any service building.",
            "All other types are only available to matching service buildings."
        ]}
    />
)

export const GroupCard = (props: { group: Group }) => {
    const { group } = props
    const [expanded, setExpanded] = useState(false)
    const [nameDraft, setNameDraft] = useState(group.name)
    const [nameFocused, setNameFocused] = useState(false)
    const selectingGroup = useValue(selectingGroup$)
    const selectingThisGroup = entityEquals(selectingGroup, group.entity)
    const dialogStack = useContext(DialogStack)

    // Deleting an unassigned group is a no-op consequence-wise; deleting one
    // that's actively managing a building's operating districts is not, so
    // that case alone gets a confirmation stop.
    const handleDeleteGroup = () => {
        if (group.assignedBuildingCount === 0) {
            trigger(mod.id, "deleteGroup", group.entity)
            return
        }
        const deleteGroupMessage =
            `"${group.name}" is assigned to ${group.assignedBuildingCount} service building(s).\n` +
            `Assigned service building(s) will serve the whole city again.`
        dialogStack.showDialog(
            <ConfirmationDialog
                title="Delete District Group?"
                message={deleteGroupMessage}
                multiline={true}
                confirm="Keep group"
                cancel="Delete group"
                onConfirm={() => dialogStack.closeAll()}
                onCancel={() => {
                    trigger(mod.id, "deleteGroup", group.entity)
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
            trigger(mod.id, "renameGroup", group.entity, trimmed)
        }
    }

    return (
        <div style={styles.groupCard}>
            <div style={styles.row}>
                <button className={css.expandButton} onClick={() => setExpanded(!expanded)}>
                    <UilIcon name={expanded ? "ArrowDownThickStroke" : "ArrowRightThickStroke"} size="12rem" />
                </button>
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
                    onChange={(t) => trigger(mod.id, "setGroupType", group.entity, t)}
                    labels={kTypeLabels}
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
                                <Tooltip tooltip="Remove the district from the group.">
                                    <button
                                        className={css.memberDeleteButton}
                                        onClick={() => trigger(mod.id, "removeMember", group.entity, member.entity)}
                                    >
                                        <UilIcon name="Trash" size="20rem" />
                                    </button>
                                </Tooltip>
                            </div>
                        ))}
                    </div>

                    <button
                        className={`${css.selectDistrictsButton} ${selectingThisGroup ? css.selectDistrictsButtonActive : ""}`}
                        onClick={() => trigger(mod.id, "toggleDistrictSelection", group.entity)}
                    >
                        <GameIcon name="Districts" size="16rem" />
                        <span style={{ marginLeft: "6rem" }}>Select Districts</span>
                    </button>
                </>
            )}
        </div>
    )
}

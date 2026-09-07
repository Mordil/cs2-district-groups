import { CSSProperties, MouseEvent, useContext, useState } from "react"

import { trigger } from "cs2/api"
import { LocalizedString } from "cs2/l10n"
import { ConfirmationDialog, DialogStack, FormattedParagraphs, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { gameIconSrc, glyphIconSrc, modIconSrc } from "../../components/icons"
import { MetadataItem } from "../../components/MetadataItem"
import { SelectDistrictsButton } from "../../components/SelectDistrictsButton"
import { VC, VF, VT } from "../../components/vanilla"
import { colorToCss, markdownRenderer } from "../../shared"
import { deleteGroup, removeMember, toggleDistrictSelection } from "../../triggers"
import { Group } from "../../types"
import { VanillaLocale, useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"
import { useEnterExitPhase } from "../../utils/useEnterExitPhase"

import css from "./GroupCard.module.scss"

const kExpandDurationMs = 250
const kBackgroundTintAlpha = 0.13
const kDividerTintAlpha = 0.5

// Tints the trash icon on the group-level delete action to flag it as the
// harder-to-reverse one; the per-member remove below stays neutral.
const dangerIconStyle = { "--iconColor": "var(--negativeColor)" } as CSSProperties
const removeButtonStyle = { "height": "24rem", "width": "24rem" } as CSSProperties

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

interface GroupCardProps {
    group: Group
    selectingDistricts: boolean
    onViewDetails: () => void
}

export const GroupCard = ({ group, selectingDistricts, onViewDetails }: GroupCardProps) => {
    const t = useTranslation()
    const [expanded, setExpanded] = useState(false)
    const { phase: expandPhase, mounted: expandedContentMounted } = useEnterExitPhase(
        expanded,
        kExpandDurationMs
    )
    const dialogStack = useContext(DialogStack)

    const deleteGroupTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("deleteGroupTooltipLine1"), t("deleteGroupTooltipLine2")]}
        />
    )

    const identityColor = colorToCss(group.color)
    const dividerTint = colorToCss(group.color, kDividerTintAlpha)
    const backgroundTint = colorToCss(group.color, kBackgroundTintAlpha)
    const backgroundTintStyle: CSSProperties = {
        backgroundImage: `linear-gradient(${backgroundTint}, ${backgroundTint})`,
    }

    // Deleting an unassigned group is pretty easy to recover from; deleting one
    // that's actively managing a building's operating districts is not, so let's get the user to double confirm
    const handleDeleteGroup = () => {
        logger.info(`Delete group clicked; entity:${entityKey(group.entity)}`)
        if (group.buildings.length === 0) {
            deleteGroup(group.entity)
            return
        }
        const deleteGroupMessage = t("deleteGroupConfirmMessage", {
            name: group.name,
            count: group.buildings.length,
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
            <div className={css.colorStripe} style={{ backgroundColor: identityColor }} />

            <div className={css.cardBody}>
                <div style={backgroundTintStyle}>
                    <div className={css.groupDetailRow}>
                        <VC.IconButton
                            tinted={true}
                            focusKey={VF.FOCUS_DISABLED}
                            src={glyphIconSrc(expanded ? "ThickStrokeArrowDown" : "ThickStrokeArrowRight")}
                            theme={VT.roundIconButton}
                            className={css.rowIconButton}
                            onSelect={toggleExpanded}
                            onMouseDown={stopMouseDown}
                        />

                        <span className={css.groupName}>{group.name}</span>

                        <div className={css.viewDetailsLink}>
                            <VC.InfoLink
                                onSelect={() => {
                                    logger.info(`View group details clicked; entity:${entityKey(group.entity)}`)
                                    onViewDetails()
                                }}
                            >
                                <LocalizedString
                                    id={VanillaLocale.details.id}
                                    fallback={VanillaLocale.details.fallback}
                                />
                            </VC.InfoLink>
                        </div>
                    </div>

                    <div className={css.metadataRow}>
                        <div className={css.metadataItems}>
                            <MetadataItem
                                icon={gameIconSrc("LotTool")}
                                value={group.members.length}
                                tooltip={t("metadataDistrictsTooltip")}
                            />
                            <MetadataItem
                                icon={modIconSrc("building")}
                                value={group.buildings.length}
                                tooltip={t("metadataBuildingsTooltip")}
                            />
                            <MetadataItem
                                icon={gameIconSrc("Population")}
                                value={group.population}
                                tooltip={t("metadataPopulationTooltip")}
                            />
                        </div>

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
                </div>

                {expandedContentMounted && (
                    <div className={`${css.expandableContent} ${css[expandPhase]}`} style={{ borderTopColor: dividerTint }}>
                        <div className={css.memberList}>
                            {group.members.map((member) => (
                                <div className={css.memberRow} key={entityKey(member.entity)}>
                                    <div className={css.memberName}>{member.name}</div>

                                    <div className={css.viewDetailsLink}>
                                        <VC.InfoLink
                                            onSelect={() => {
                                                logger.info(`View district details clicked; entity:${entityKey(group.entity)} member:${entityKey(member.entity)}`)
                                                trigger("selectedInfo", "selectEntity", member.entity)
                                            }}
                                        >
                                            <LocalizedString
                                                id={VanillaLocale.details.id}
                                                fallback={VanillaLocale.details.fallback}
                                            />
                                        </VC.InfoLink>
                                    </div>

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

                        <SelectDistrictsButton
                            selected={selectingDistricts}
                            className={css.selectDistrictButton}
                            onSelect={() => {
                                logger.info(`Toggle district selection clicked; entity:${entityKey(group.entity)}`)
                                toggleDistrictSelection(group.entity)
                            }}
                        />
                    </div>
                )}
            </div>
        </div>
    )
}

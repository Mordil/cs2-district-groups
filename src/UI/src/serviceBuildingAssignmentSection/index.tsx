import { trigger } from "cs2/api"
import { getModule } from "cs2/modding"
import { Dropdown, DropdownToggle, FormattedParagraphs } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import mod from "../../mod.json"
import { useTypeLabels } from "../constants"
import { useTranslation } from "../locale"
import { markdownRenderer } from "../shared"
import { logger } from "../log"
import selectorCss from "../components/selectorToggle.module.scss"

interface GroupOption {
    entity: Entity
    name: string
    type: number
}

const kGenericType = 0
const kNoneEntity: Entity = { index: 0, version: 0 }

const InfoSection: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
)
const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
)

// Same vanilla dropdown internals used by the group manager panel.
const dropdownTheme: any = getModule("game-ui/editor/themes/editor-dropdown.module.scss", "classes")
const DropdownItem: any = getModule("game-ui/common/input/dropdown/items/dropdown-item.tsx", "DropdownItem")

// Matching-type groups first (alphabetized), then Generic groups (alphabetized).
const sortCandidates = (candidates: GroupOption[], buildingType: number): GroupOption[] =>
    [...candidates].sort((a, b) => {
        const aMatches = a.type === buildingType
        const bMatches = b.type === buildingType
        if (aMatches !== bMatches) {
            return aMatches ? -1 : 1
        }
        return a.name.localeCompare(b.name)
    })

const Section = (props: any) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const buildingType: number = props.buildingType ?? kGenericType
    const candidates: GroupOption[] = sortCandidates(props.candidates ?? [], buildingType)
    const hasAssignment: boolean = props.hasAssignment ?? false
    const assignedGroupName: string = props.assignedGroupName ?? ""

    const sectionTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                t("sectionTooltipLine1"),
                t("sectionTooltipLine2"),
                t("sectionTooltipLine3"),
                t("sectionTooltipLine4"),
            ]}
        />
    )

    return (
        <InfoSection disableFocus={true} tooltip={sectionTooltip}>
            <InfoRow
                left={t("sectionLabel")}
                right={
                    <Dropdown
                        theme={dropdownTheme}
                        content={[
                            <DropdownItem
                                key="none"
                                value={kNoneEntity}
                                className={dropdownTheme.dropdownItem}
                                selected={!hasAssignment}
                                closeOnSelect={true}
                                onChange={() => {
                                    logger.info("Unassign group clicked;")
                                    trigger(mod.id, "unassignGroup")
                                }}
                            >
                                <div>{t("unassignOption")}</div>
                            </DropdownItem>,
                            ...candidates.map((candidate) => (
                                <DropdownItem
                                    key={entityKey(candidate.entity)}
                                    value={candidate.entity}
                                    className={dropdownTheme.dropdownItem}
                                    closeOnSelect={true}
                                    onChange={() => {
                                        logger.info(`Assign group clicked; entity:${entityKey(candidate.entity)}`)
                                        trigger(mod.id, "assignGroup", candidate.entity)
                                    }}
                                >
                                    <div>{`${candidate.name} (${typeLabels[candidate.type] ?? "?"})`}</div>
                                </DropdownItem>
                            )),
                        ]}
                    >
                        <DropdownToggle
                            disabled={false}
                            openIconComponent={<></>}
                            closeIconComponent={<></>}
                            className={selectorCss.selectorToggle}
                        >
                            <div>{hasAssignment ? assignedGroupName : t("unassignedLabel")}</div>
                        </DropdownToggle>
                    </Dropdown>
                }
                uppercase={true}
                disableFocus={true}
            />
        </InfoSection>
    )
}

// Middleware for the vanilla selected-info panel: the key must be the FULL C#
// type name of the InfoSectionBase system that writes this section's data.
export const DistrictGroupSectionComponent = (componentList: any): any => {
    componentList["DistrictGroups.DistrictGroupSection"] = (props: any) => <Section {...props} />
    return componentList
}

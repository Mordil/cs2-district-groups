import { trigger } from "cs2/api"
import { FormattedParagraphs } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import mod from "../../mod.json"
import { GroupSelector } from "../components/groupSelector"
import { VC } from "../components/vanilla"
import { useTranslation } from "../locale"
import { logger } from "../log"
import { markdownRenderer } from "../shared"
import css from "./index.module.scss"

const kGenericType = 0

const Section = (props: any) => {
    const t = useTranslation()
    const buildingType: number = props.buildingType ?? kGenericType
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

    // Both triggers act on whatever building the info panel has selected, so the section never has to name it
    const onSelect = (group: Entity) => {
        logger.info(`Assign group clicked; group:${entityKey(group)}`)
        trigger(mod.id, "assignGroup", group)
    }

    const onUnassign = () => {
        logger.info("Unassign group clicked;")
        trigger(mod.id, "unassignGroup")
    }

    return (
        <VC.InfoSection disableFocus={true} tooltip={sectionTooltip}>
            <VC.InfoRow
                left={<span style={{ flexShrink: 0, whiteSpace: "nowrap" }}>{t("sectionLabel")}</span>}
                right={
                    <GroupSelector
                        buildingType={buildingType}
                        candidates={props.candidates ?? []}
                        hasAssignment={hasAssignment}
                        assignedGroupName={assignedGroupName}
                        onSelect={onSelect}
                        onUnassign={onUnassign}
                        className={css.groupSelector}
                    />
                }
                uppercase={true}
                disableFocus={false}
            />
        </VC.InfoSection>
    )
}

// Middleware for the vanilla selected-info panel: the key must be the FULL C#
// type name of the InfoSectionBase system that writes this section's data.
export const DistrictGroupSectionComponent = (componentList: any): any => {
    componentList["DistrictGroups.DistrictGroupSection"] = (props: any) => <Section {...props} />
    return componentList
}

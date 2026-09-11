import { LocalizedString } from "cs2/l10n"
import { FormattedParagraphs } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"

import { VC } from "../../components/vanilla"
import { kTypeIcons } from "../../constants"
import { requestGroupInspection } from "../../inspectGroup"
import { markdownRenderer } from "../../shared"
import { VanillaLocale, useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"

// One district group the selected district belongs to.
interface MemberGroup {
    entity: Entity
    name: string
    type: number
}

const Section = (props: any) => {
    const t = useTranslation()
    const groups: MemberGroup[] = props.groups ?? []

    const sectionTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("membershipSectionTooltipLine1"), t("membershipSectionTooltipLine2")]}
        />
    )

    const sortedGroups = [...groups].sort((a, b) => a.name.localeCompare(b.name))

    const onInspect = (group: MemberGroup) => {
        logger.info(`Inspect group clicked; entity:${entityKey(group.entity)}`)
        requestGroupInspection(group.entity)
    }

    return (
        <VC.InfoSection disableFocus={true} tooltip={sectionTooltip}>
            <VC.InfoRow uppercase={true} disableFocus={true} left={t("membershipSectionLabel")} />
            {sortedGroups.map((group) => (
                <VC.InfoRow
                    key={entityKey(group.entity)}
                    subRow={true}
                    disableFocus={true}
                    icon={kTypeIcons[group.type]}
                    left={group.name}
                    link={
                        <VC.InfoLink onSelect={() => onInspect(group)}>
                            <LocalizedString
                                id={VanillaLocale.details.id}
                                fallback={VanillaLocale.details.fallback}
                            />
                        </VC.InfoLink>
                    }
                />
            ))}
        </VC.InfoSection>
    )
}

// Middleware for the vanilla selected-info panel
// the key must be the FULL C# type name of the InfoSectionBase system that writes this section's data.
export const GroupMembershipSectionComponent = (componentList: any): any => {
    componentList["DistrictGroups.DistrictGroupMembershipSection"] = (props: any) => <Section {...props} />
    return componentList
}

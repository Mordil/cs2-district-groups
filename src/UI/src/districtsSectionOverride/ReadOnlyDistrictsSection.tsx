import { trigger } from "cs2/api"
import { LocalizedEntityName, LocalizedString, Name } from "cs2/l10n"
import { Entity } from "cs2/utils"
import { FormattedParagraphs } from "cs2/ui"
import { VC } from "../components/vanilla"
import { VanillaLocale, useTranslation } from "../locale"
import { markdownRenderer } from "../shared"

interface VanillaDistrictEntry {
    entity: Entity
    name: Name
}

export interface VanillaDistrictsSectionProps {
    districts: VanillaDistrictEntry[]
}

// Trimmed replacement for the vanilla DistrictsSection: drops the "Select operating
// districts" button and per-row trash icon, since a group-assigned building's
// ServiceDistrict buffer is managed by the group, not by manual editing.
export const ReadOnlyDistrictsSection = (props: VanillaDistrictsSectionProps) => {
    const t = useTranslation()

    const readOnlyTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                t("readOnlySectionTooltipLine1"),
                t("readOnlySectionTooltipLine2"),
                t("readOnlySectionTooltipLine3"),
            ]}
        />
    )

    return (
        <VC.InfoSection disableFocus={true} tooltip={readOnlyTooltip}>
            <VC.InfoRow uppercase={true} disableFocus={true} left={t("operatingDistrictsLabel")} />
            {props.districts.map((district) => (
                <VC.InfoRow
                    key={district.entity.index}
                    subRow={true}
                    disableFocus={true}
                    left={<LocalizedEntityName value={district.name} />}
                    link={
                        <VC.InfoLink onSelect={() => trigger("selectedInfo", "selectEntity", district.entity)}>
                            <LocalizedString id={VanillaLocale.details.id} fallback={VanillaLocale.details.fallback}/>
                        </VC.InfoLink>
                    }
                />
            ))}
        </VC.InfoSection>
    )
}

import { useValue } from "cs2/api"
import { logger } from "../../utils/log"
import { selectedBuildingHasGroupAssignment$ } from "../../bindings"
import { ReadOnlyDistrictsSection, VanillaDistrictsSectionProps } from "./ReadOnlyDistrictsSection"

// Sections are keyed by the full C# type name of the InfoSectionBase system that writes the section's data
const kVanillaDistrictsSectionKey = "Game.UI.InGame.DistrictsSection"

// Replaces vanilla's DistrictsSection with a read-only render whenever the
// selected building is group-assigned, since the group owns its ServiceDistrict
// buffer at that point; ungrouped buildings keep full vanilla manual control.
const createDistrictsSectionOverride = (VanillaDistrictsSection: any) => (props: VanillaDistrictsSectionProps) => {
    const isGrouped = useValue(selectedBuildingHasGroupAssignment$)

    logger.debug(`DistrictsSection override rendered; isGrouped:${isGrouped}`)

    if (isGrouped) {
        return <ReadOnlyDistrictsSection {...props} />
    }
    return <VanillaDistrictsSection {...props} />
}

// Wraps the vanilla entry in place within the shared componentList, mirroring
// DistrictGroupSectionComponent's mutate-and-return-componentList contract.
export const wrapVanillaDistrictsSection = (componentList: any): any => {
    const VanillaDistrictsSection = componentList?.[kVanillaDistrictsSectionKey]
    logger.debug(`Wrapping vanilla DistrictsSection entry; found:${VanillaDistrictsSection != null}`)
    if (VanillaDistrictsSection) {
        componentList[kVanillaDistrictsSectionKey] = createDistrictsSectionOverride(VanillaDistrictsSection)
    }
    return componentList
}

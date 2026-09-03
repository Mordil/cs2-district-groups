import { trigger, useValue } from "cs2/api"
import { Scrollable } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import mod from "../../mod.json"
import { eligibleGroups, GroupSelector } from "../components/groupSelector"
import { kGenericType } from "../constants"
import { useTranslation } from "../locale"
import { logger } from "../log"
import { ServiceBuilding } from "../types"
import { groups$, serviceBuildings$ } from "./bindings"
import css from "./AssignmentsTab.module.scss"

const emptyTextStyle = { fontSize: "var(--fontSizeM)" }

interface AssignmentsTabProps {
    filterType: number
    hideAssigned: boolean
    className?: string
}

export const AssignmentsTab = ({ filterType, hideAssigned, className }: AssignmentsTabProps) => {
    const t = useTranslation()
    const buildings = useValue(serviceBuildings$)
    const groups = useValue(groups$)

    const onSelect = (building: ServiceBuilding, group: Entity) => {
        logger.info(`Assign group clicked; building:${entityKey(building.entity)} group:${entityKey(group)}`)
        trigger(mod.id, "assignBuildingGroup", building.entity, group)
    }

    const onUnassign = (building: ServiceBuilding) => {
        logger.info(`Unassign group clicked; building:${entityKey(building.entity)}`)
        trigger(mod.id, "unassignBuildingGroup", building.entity)
    }

    // filter() already copies the array, so sorting the result in place is safe.
    const displayedBuildings = buildings
        .filter((building) => !hideAssigned || !building.hasAssignment)
        .sort((a, b) => a.name.localeCompare(b.name))

    return (
        <Scrollable
            vertical={true}
            trackVisibility="reserve"
            className={className}
        >
            {filterType === kGenericType && (
                <div style={emptyTextStyle}>
                    {t("selectTypeForAssignments")}
                </div>
            )}

            {filterType !== kGenericType && displayedBuildings.length === 0 && (
                <div style={emptyTextStyle}>
                    {t("noServiceBuildingsMatchFilter")}
                </div>
            )}

            {filterType !== kGenericType &&
                displayedBuildings.map((building) => (
                    <div key={entityKey(building.entity)} className={css.row}>
                        <div className={css.buildingName}>{building.name}</div>

                        <GroupSelector
                            buildingType={building.type}
                            candidates={eligibleGroups(groups, building.type, building.assignedGroup)}
                            hasAssignment={building.hasAssignment}
                            assignedGroupName={building.assignedGroupName}
                            onSelect={(group) => onSelect(building, group)}
                            onUnassign={() => onUnassign(building)}
                            className={css.groupSelector}
                        />
                    </div>
                ))}
        </Scrollable>
    )
}

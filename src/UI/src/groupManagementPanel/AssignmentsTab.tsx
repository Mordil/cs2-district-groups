import { trigger, useValue } from "cs2/api"
import { Scrollable } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import mod from "../../mod.json"
import { eligibleGroups, GroupSelector } from "../components/groupSelector"
import { useTranslation } from "../locale"
import { logger } from "../log"
import { ServiceBuilding } from "../types"
import { groups$, serviceBuildings$ } from "./bindings"
import css from "./AssignmentsTab.module.scss"

const kGenericType = 0

const emptyTextStyle = { fontSize: "var(--fontSizeM)" }

interface AssignmentsTabProps {
    filterType: number
    className?: string
}

export const AssignmentsTab = ({ filterType, className }: AssignmentsTabProps) => {
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

    const sortedBuildings = [...buildings].sort((a, b) => a.name.localeCompare(b.name))

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

            {filterType !== kGenericType && buildings.length === 0 && (
                <div style={emptyTextStyle}>
                    {t("noServiceBuildingsMatchFilter")}
                </div>
            )}

            {filterType !== kGenericType &&
                sortedBuildings.map((building) => (
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

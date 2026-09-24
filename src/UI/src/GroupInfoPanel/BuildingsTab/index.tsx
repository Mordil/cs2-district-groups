import { entityKey } from "cs2/utils"

import { DataTable, EmptyTable } from "../../components/DataTable"
import { useTypeLabels } from "../../constants"
import { buildingsColumns } from "../../groupStats/columns"
import { unassignBuildingGroup } from "../../triggers"
import { AssignedBuilding, Group } from "../../types"
import { useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"

interface BuildingsTabProps {
    group: Group
    className?: string
}

// Lists the service buildings assigned to the group as a sortable table, under the columns its service type reads out.
export const BuildingsTab = ({ group, className }: BuildingsTabProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()

    if (group.buildings.length === 0) {
        return <EmptyTable message={t("noBuildingsInGroup")} className={className} />
    }

    const onRemoveBuilding = (building: AssignedBuilding) => {
        logger.info(
            `Remove building clicked; entity:${entityKey(group.entity)} building:${entityKey(building.entity)}`
        )
        unassignBuildingGroup(building.entity)
    }

    return (
        <DataTable
            id="buildings"
            columns={buildingsColumns(group.type, typeLabels)}
            rows={group.buildings}
            entityOf={(building) => building.entity}
            onRemove={onRemoveBuilding}
            removeTooltip={t("removeBuildingTooltip")}
            className={className}
        />
    )
}

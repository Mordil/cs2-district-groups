import { entityKey } from "cs2/utils"

import { DataTable, EmptyTable } from "../../components/DataTable"
import { overviewColumns } from "../../groupStats/columns"
import { removeMember } from "../../triggers"
import { DistrictMember, Group } from "../../types"
import { useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"

interface OverviewTabProps {
    group: Group
    className?: string
}

// Lists the districts that belong to the group as a sortable table, under the columns its service type reads out.
export const OverviewTab = ({ group, className }: OverviewTabProps) => {
    const t = useTranslation()

    if (group.members.length === 0) {
        return <EmptyTable message={t("noDistrictsInGroup")} className={className} />
    }

    const onRemoveMember = (member: DistrictMember) => {
        logger.debug(
            `Remove member clicked; entity:${entityKey(group.entity)} member:${entityKey(member.entity)}`
        )
        removeMember(group.entity, member.entity)
    }

    return (
        <DataTable
            id="districts"
            columns={overviewColumns(group.type)}
            rows={group.members}
            entityOf={(member) => member.entity}
            total={group}
            onRemove={onRemoveMember}
            removeTooltip={t("removeMemberTooltip")}
            className={className}
        />
    )
}

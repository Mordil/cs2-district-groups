import { useValue } from "cs2/api"
import { Scrollable } from "cs2/ui"
import { entityEquals, entityKey } from "cs2/utils"
import { useTranslation } from "../../utils/locale"
import { groups$, selectingGroup$ } from "../../bindings"
import { GroupCard } from "./GroupCard"

interface GroupManagementTabProps {
    filterType: number
    className?: string
}

export const GroupManagementTab = ({ filterType, className }: GroupManagementTabProps) => {
    const t = useTranslation()
    const groups = useValue(groups$)
    const selectingGroup = useValue(selectingGroup$)

    // Groups matching the filtered type, in creation order (the binding's own order).
    const displayedGroups = groups.filter((g) => g.type === filterType)

    return (
        <Scrollable
            vertical={true}
            trackVisibility="reserve"
            className={className}
        >
            {groups.length === 0 && (
                <div>{t("noGroupsYet")}</div>
            )}

            {groups.length > 0 && displayedGroups.length === 0 && (
                <div>{t("noGroupsMatchFilter")}</div>
            )}

            {displayedGroups.map((group) => (
                <GroupCard
                    key={entityKey(group.entity)}
                    group={group}
                    selectingDistricts={entityEquals(selectingGroup, group.entity)}
                />
            ))}
        </Scrollable>
    )
}

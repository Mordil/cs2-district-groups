import { Scrollable } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { Group } from "../../types"

import css from "./index.module.scss"

interface OverviewTabProps {
    group: Group
    className?: string
}

// Lists the districts that belong to the group.
export const OverviewTab = ({ group, className }: OverviewTabProps) => (
    <Scrollable vertical={true} trackVisibility="reserve" className={className}>
        {group.members.map((member) => (
            <div key={entityKey(member.entity)} className={css.listItem}>
                <span className={css.listItemName}>{member.name}</span>
            </div>
        ))}
    </Scrollable>
)

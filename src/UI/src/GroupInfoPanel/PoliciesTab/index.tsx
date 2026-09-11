import { useState } from "react"

import { Scrollable } from "cs2/ui"

import { Group, GroupPolicy } from "../../types"
import { useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"

import { PolicyRow } from "./PolicyRow"
import css from "./index.module.scss"

interface PoliciesTabProps {
    group: Group
    policies: GroupPolicy[]
    className?: string
}

// Lists every district policy the game has, set for the group at once or district by district.
export const PoliciesTab = ({ group, policies, className }: PoliciesTabProps) => {
    const t = useTranslation()
    const [expanded, setExpanded] = useState<string[]>([])

    if (group.members.length === 0) {
        return <div className={`${css.empty} ${className ?? ""}`}>{t("noDistrictsInGroup")}</div>
    }

    const onToggleExpanded = (policy: GroupPolicy) => {
        const isExpanded = expanded.includes(policy.id)
        logger.info(`Policy row expansion toggled; policy:${policy.id} expanded:${!isExpanded}`)
        setExpanded(
            isExpanded ? expanded.filter((id) => id !== policy.id) : [...expanded, policy.id]
        )
    }

    return (
        <div className={`${css.tab} ${className ?? ""}`}>
            <Scrollable vertical={true} trackVisibility="reserve" className={css.list}>
                {policies.map((policy) => (
                    <PolicyRow
                        key={policy.id}
                        group={group}
                        policy={policy}
                        expanded={expanded.includes(policy.id)}
                        onToggleExpanded={() => onToggleExpanded(policy)}
                    />
                ))}
            </Scrollable>
        </div>
    )
}

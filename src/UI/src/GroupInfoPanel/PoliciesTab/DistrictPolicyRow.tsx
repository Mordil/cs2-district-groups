import { entityKey } from "cs2/utils"

import { Checkbox } from "../../components/Checkbox"
import { setDistrictPolicyActive, setDistrictPolicyValue } from "../../triggers"
import { DistrictPolicyState, GroupPolicy } from "../../types"
import { logger } from "../../utils/log"

import { PolicyValueSlider } from "./PolicyValueSlider"
import css from "./index.module.scss"

interface DistrictPolicyRowProps {
    policy: GroupPolicy
    district: DistrictPolicyState
}

// One member district's own setting for a policy, listed when its policy row is expanded.
export const DistrictPolicyRow = ({ policy, district }: DistrictPolicyRowProps) => {
    const onToggle = (active: boolean) => {
        logger.info(`District policy toggled; district:${entityKey(district.entity)} policy:${policy.id} active:${active}`)
        setDistrictPolicyActive(district.entity, policy.entity, active)
    }

    const onValue = (value: number) => {
        logger.info(`District policy value changed; district:${entityKey(district.entity)} policy:${policy.id} value:${value}`)
        setDistrictPolicyValue(district.entity, policy.entity, value)
    }

    return (
        <div className={css.districtRow}>
            <div className={css.districtHeader}>
                <Checkbox
                    checked={district.active}
                    onChange={onToggle}
                    label={district.name}
                    className={css.districtToggle}
                />

                <div className={css.caretSpacer} />
            </div>

            {policy.slider !== null && district.active && (
                <PolicyValueSlider slider={policy.slider} value={district.value} onCommit={onValue} />
            )}
        </div>
    )
}

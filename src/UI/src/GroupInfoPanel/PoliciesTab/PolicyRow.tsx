import { LocalizedFraction, LocalizedString, Unit, useLocalization } from "cs2/l10n"
import { Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { Checkbox } from "../../components/Checkbox"
import { glyphIconSrc } from "../../components/icons"
import { VC, VF, VT } from "../../components/vanilla"
import { setGroupPolicyActive, setGroupPolicyValue } from "../../triggers"
import { DistrictPolicyState, Group, GroupPolicy } from "../../types"
import { useTranslation } from "../../utils/locale"
import { logger } from "../../utils/log"

import { DistrictPolicyRow } from "./DistrictPolicyRow"
import { PolicyValueSlider } from "./PolicyValueSlider"
import css from "./index.module.scss"

// Policy names and descriptions live in the game's own locale dictionary, keyed on the prefab name.
const policyTitleId = (id: string) => `Policy.TITLE[${id}]`
const policyDescriptionId = (id: string) => `Policy.DESCRIPTION[${id}]`

// What the districts of a group add up to for one policy.
interface PolicyTally {
    total: number
    activeCount: number
    // Undefined once the districts carrying the policy disagree, which is all the group row can say.
    sharedValue: number | undefined
    // A real value out of the group, so a group slider with nothing shared still sits on something true.
    lowestValue: number
}

const tally = (districts: DistrictPolicyState[]): PolicyTally => {
    const active = districts.filter((district) => district.active)
    const values = active.map((district) => district.value)
    const shared = values.length > 0 && values.every((value) => value === values[0])

    return {
        total: districts.length,
        activeCount: active.length,
        sharedValue: shared ? values[0] : undefined,
        lowestValue: values.length > 0 ? Math.min(...values) : 0,
    }
}

interface PolicyRowProps {
    group: Group
    policy: GroupPolicy
    expanded: boolean
    onToggleExpanded: () => void
}

// One policy across the whole group: what its districts add up to, over the districts themselves.
export const PolicyRow = ({ group, policy, expanded, onToggleExpanded }: PolicyRowProps) => {
    const t = useTranslation()
    // Not every policy ships a description, and an empty tooltip is worse than none at all.
    const { translate } = useLocalization()
    const description = translate(policyDescriptionId(policy.id), null)
    const counts = tally(policy.districts)
    const allActive = counts.total > 0 && counts.activeCount === counts.total
    const someActive = counts.activeCount > 0 && !allActive

    const title = (
        <div className={css.policyTitle}>
            <img
                className={`${css.icon} ${counts.activeCount === 0 ? css.iconOff : ""}`}
                src={policy.icon}
            />
            <div className={css.policyName}>
                <LocalizedString id={policyTitleId(policy.id)} fallback={policy.id} />
            </div>
        </div>
    )

    /*
        The box reads all three states but only ever writes two: "some" is what the districts happen
        to add up to, never something a click can ask for, so a click means "make them all agree".
    */
    const onToggle = () => {
        logger.info(`Group policy toggled; group:${entityKey(group.entity)} policy:${policy.id} active:${!allActive}`)
        setGroupPolicyActive(group.entity, policy.entity, !allActive)
    }

    const onValue = (value: number) => {
        logger.info(`Group policy value changed; group:${entityKey(group.entity)} policy:${policy.id} value:${value}`)
        setGroupPolicyValue(group.entity, policy.entity, value)
    }

    const toggleTooltip = allActive
        ? t("clearPolicyFromGroupTooltip", { count: counts.total })
        : t("applyPolicyToGroupTooltip", { count: counts.total })

    return (
        <div className={css.policyRow}>
            <div className={css.policyHeader}>
                <Tooltip tooltip={description ?? undefined}>{title}</Tooltip>

                <div className={css.count}>
                    <LocalizedFraction
                        value={counts.activeCount}
                        total={counts.total}
                        unit={Unit.Integer}
                    />
                </div>

                <Checkbox
                    checked={allActive ? true : someActive ? undefined : false}
                    multistate={true}
                    onMultistateChange={onToggle}
                    tooltip={toggleTooltip}
                    className={css.checkbox}
                />

                <VC.IconButton
                    tinted={true}
                    focusKey={VF.FOCUS_DISABLED}
                    theme={VT.actionButton}
                    className={css.caret}
                    src={glyphIconSrc(expanded ? "ThickStrokeArrowDown" : "ThickStrokeArrowRight")}
                    onSelect={onToggleExpanded}
                />
            </div>

            {policy.slider !== null && counts.activeCount > 0 && (
                <PolicyValueSlider
                    slider={policy.slider}
                    value={counts.sharedValue ?? counts.lowestValue}
                    mixed={counts.sharedValue === undefined}
                    onCommit={onValue}
                />
            )}

            {expanded && (
                <div className={css.districts}>
                    {policy.districts.map((district) => (
                        <DistrictPolicyRow
                            key={entityKey(district.entity)}
                            policy={policy}
                            district={district}
                        />
                    ))}
                </div>
            )}
        </div>
    )
}

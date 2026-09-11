import { ReactNode } from "react"

import { Dropdown, Tooltip } from "cs2/ui"

import css from "./GroupTypeSelector.module.scss"
import { SrcIcon } from "./icons"
import { VC, VT } from "./vanilla"

// A labeled-option dropdown picker over a district group's service type, shown as an icon + ellipsized label toggle; `value` is an index into `labels`.
export const GroupTypeSelector = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    iconSrc: string
    optionIcons: string[]
    tooltip?: ReactNode
    className?: string
}) => {
    const label = props.labels[props.value] ?? "?"

    const selector = (
        // key forces a full remount on every selection: closeOnSelect closing the
        // dropdown appears to desync the surrounding Tooltip's hover wiring for
        // the existing instance; a fresh mount sidesteps it entirely.
        <Tooltip key={props.value} tooltip={props.tooltip}>
            <Dropdown
                theme={VT.editorDropdown}
                content={props.labels.map((optionLabel, i) => (
                    <VC.DropdownItem
                        key={i}
                        value={i}
                        className={VT.editorDropdown.dropdownItem}
                        selected={i === props.value}
                        closeOnSelect={true}
                        onChange={() => props.onChange(i)}
                    >
                        <div className={css.optionRow}>
                            <SrcIcon src={props.optionIcons[i]} size="20rem" />

                            <div className={css.optionLabel}>{optionLabel}</div>
                        </div>
                    </VC.DropdownItem>
                ))}
            >
                <VC.DropdownToggleBase
                    disabled={false}
                    className={[css.selectorToggle, props.className].filter(Boolean).join(" ")}
                >
                    <div className={css.iconSlot}>
                        <SrcIcon src={props.iconSrc} size="24rem" />
                    </div>
                    <div className={css.selectorLabel}>{label}</div>
                </VC.DropdownToggleBase>
            </Dropdown>
        </Tooltip>
    )

    return selector
}

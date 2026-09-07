import { ReactNode } from "react"

import { Dropdown, Tooltip } from "cs2/ui"

import css from "./GroupTypeSelector.module.scss"
import { ModIcon } from "./icons"
import { VC, VT } from "./vanilla"

// A labeled-option dropdown picker over a district group's service type, shown as an icon + ellipsized label toggle; `value` is an index into `labels`.
export const GroupTypeSelector = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    icon: string
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
                        <div style={{
                            paddingLeft: "8rem",
                            paddingRight: "8rem"
                        }}>{optionLabel}</div>
                    </VC.DropdownItem>
                ))}
            >
                <VC.DropdownToggleBase
                    disabled={false}
                    className={[css.selectorToggle, props.className].filter(Boolean).join(" ")}
                >
                    <div className={css.iconSlot}>
                        <ModIcon name={props.icon} />
                    </div>
                    <div className={css.selectorLabel}>{label}</div>
                </VC.DropdownToggleBase>
            </Dropdown>
        </Tooltip>
    )

    return selector
}

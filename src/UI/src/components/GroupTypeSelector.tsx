import { CSSProperties, ReactNode } from "react"

import { Dropdown, DropdownToggle, Tooltip } from "cs2/ui"

import css from "./GroupTypeSelector.module.scss"
import { ModIcon } from "./icons"
import { VC, VT } from "./vanilla"

// A labeled-option dropdown picker over a district group's service type; `value` is an index into `labels`.
// Pass `icon` to render it as a filter-style toggle instead of a plain label.
export const GroupTypeSelector = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    icon?: string
    tooltip?: ReactNode
    style?: CSSProperties
}) => {
    const label = props.labels[props.value] ?? "?"

    const toggleContent = props.icon ? (
        <div className={css.filterToggleRow}>
            <ModIcon name={props.icon} />
            <span className={css.filterToggleLabel}>{label}</span>
        </div>
    ) : (
        label
    )

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
                <DropdownToggle
                    disabled={false}
                    openIconComponent={<></>}
                    closeIconComponent={<></>}
                    className={css.selectorToggle}
                    style={props.style}
                >
                    {toggleContent}
                </DropdownToggle>
            </Dropdown>
        </Tooltip>
    )

    return props.icon ? <div className={css.filterToggleContainer}>{selector}</div> : selector
}

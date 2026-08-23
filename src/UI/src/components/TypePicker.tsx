import { Dropdown, DropdownToggle, Tooltip } from "cs2/ui"
import { CSSProperties, ReactNode } from "react"
import { UilIcon } from "./icons"
import selectorCss from "./selectorToggle.module.scss"
import { VC, VT } from "./vanilla"

// A labeled-option dropdown picker: `value` is an index into `labels`.
export const TypePicker = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    tooltip?: ReactNode
    style?: CSSProperties
}) => (
    // key forces a full remount on every selection: closeOnSelect closing the
    // dropdown appears to desync the surrounding Tooltip's hover wiring for
    // the existing instance; a fresh mount sidesteps it entirely.
    <Tooltip key={props.value} tooltip={props.tooltip}>
        <Dropdown
            theme={VT.editorDropdown}
            content={props.labels.map((label, i) => (
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
                    }}>{label}</div>
                </VC.DropdownItem>
            ))}
        >
            <DropdownToggle
                disabled={false}
                openIconComponent={<></>}
                closeIconComponent={<></>}
                className={selectorCss.selectorToggle}
                style={{
                    display: "flex",
                    alignItems: "center",
                    ...props.style,
                }}
            >
                <div>{props.labels[props.value] ?? "?"}</div>
            </DropdownToggle>
        </Dropdown>
    </Tooltip>
)

// Sentinel value for TypeFilterPicker meaning "no filter" — every real
// option occupies labels[0..n-1], so -1 is free to mean "show everything".
export const kAllTypes = -1

// TypePicker plus a filter icon and, when allLabel is given, a leading "all" entry
// (value kAllTypes). Pass allLabel={null} to restrict the picker to real types only.
export const TypeFilterPicker = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    allLabel: string | null
    tooltip?: ReactNode
}) => {
    const options = props.allLabel !== null ? [props.allLabel, ...props.labels] : props.labels
    return (
        // See TypePicker's identical key comment above — same fix, same reason.
        <Tooltip key={props.value} tooltip={props.tooltip}>
            <Dropdown
                theme={VT.editorDropdown}
                content={options.map((label, i) => {
                    const value = props.allLabel !== null ? i - 1 : i
                    return (
                        <VC.DropdownItem
                            key={value}
                            value={value}
                            className={VT.editorDropdown.dropdownItem}
                            selected={value === props.value}
                            closeOnSelect={true}
                            onChange={() => props.onChange(value)}
                        >
                            <div style={{
                                paddingLeft: "8rem",
                                paddingRight: "8rem"
                            }}>{label}</div>
                        </VC.DropdownItem>
                    )
                })}
            >
                <DropdownToggle
                    disabled={false}
                    openIconComponent={<></>}
                    closeIconComponent={<></>}
                    className={selectorCss.selectorToggle}
                >
                    <div style={{ display: "flex", alignItems: "center" }}>
                        <UilIcon name="FunnelFilter"/>
                        <span style={{ marginLeft: "5rem" }}>
                            {props.value === kAllTypes && props.allLabel !== null
                                ? props.allLabel
                                : props.labels[props.value] ?? "?"}
                        </span>
                    </div>
                </DropdownToggle>
            </Dropdown>
        </Tooltip>
    )
}

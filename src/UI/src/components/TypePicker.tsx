import { getModule } from "cs2/modding"
import { Dropdown, DropdownToggle, Tooltip } from "cs2/ui"
import { CSSProperties, ReactNode } from "react"
import { UilIcon } from "./icons"
import selectorCss from "./selectorToggle.module.scss"

// Vanilla dropdown internals (Recolor's pattern): the item component and the
// editor theme aren't exported by cs2/ui, so pull them from the module registry.
const dropdownTheme: any = getModule("game-ui/editor/themes/editor-dropdown.module.scss", "classes")
const DropdownItem: any = getModule("game-ui/common/input/dropdown/items/dropdown-item.tsx", "DropdownItem")

// A labeled-option dropdown picker: `value` is an index into `labels`. The
// game's own Dropdown anchors its menu as a popup, so it overlays whatever
// it's placed in instead of being clipped by a scroll container.
export const TypePicker = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    tooltip?: ReactNode
    style?: CSSProperties
}) => (
    // key forces a full remount on every selection: closeOnSelect closing the
    // dropdown appears to desync the surrounding Tooltip's hover wiring for
    // the existing instance (same Dropdown mount-lifecycle fragility already
    // seen elsewhere this session) — a fresh mount sidesteps it entirely.
    <Tooltip key={props.value} tooltip={props.tooltip}>
        <Dropdown
            theme={dropdownTheme}
            content={props.labels.map((label, i) => (
                <DropdownItem
                    key={i}
                    value={i}
                    className={dropdownTheme.dropdownItem}
                    selected={i === props.value}
                    closeOnSelect={true}
                    onChange={() => props.onChange(i)}
                >
                    <div>{label}</div>
                </DropdownItem>
            ))}
        >
            <DropdownToggle
                disabled={false}
                openIconComponent={<></>}
                closeIconComponent={<></>}
                className={selectorCss.selectorToggle}
                style={{
                    height: "22rem",
                    boxSizing: "border-box",
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

// TypePicker plus a leading "all" entry (value kAllTypes) and a filter icon —
// the shape of a category filter control, distinct from TypePicker (which
// only offers real options, e.g. for assigning a single type to one item).
export const TypeFilterPicker = (props: {
    value: number
    onChange: (value: number) => void
    labels: string[]
    allLabel: string
    tooltip?: ReactNode
}) => (
    // See TypePicker's identical key comment above — same fix, same reason.
    <Tooltip key={props.value} tooltip={props.tooltip}>
        <Dropdown
            theme={dropdownTheme}
            content={[props.allLabel, ...props.labels].map((label, i) => {
                const value = i - 1
                return (
                    <DropdownItem
                        key={value}
                        value={value}
                        className={dropdownTheme.dropdownItem}
                        selected={value === props.value}
                        closeOnSelect={true}
                        onChange={() => props.onChange(value)}
                    >
                        <div>{label}</div>
                    </DropdownItem>
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
                    <UilIcon name="FunnelFilter" size="12rem" />
                    <span style={{ marginLeft: "5rem" }}>
                        {props.value === kAllTypes ? props.allLabel : props.labels[props.value] ?? "?"}
                    </span>
                </div>
            </DropdownToggle>
        </Dropdown>
    </Tooltip>
)

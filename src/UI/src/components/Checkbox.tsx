import { CSSProperties, ReactNode } from "react"

import { Tooltip } from "cs2/ui"

import { VT } from "./vanilla"

interface CheckboxBaseProps {
    label?: string
    tooltip?: ReactNode
    className?: string
    style?: CSSProperties
}

// A plain on/off checkbox, reporting the value it was clicked into.
interface TwoStateCheckboxProps extends CheckboxBaseProps {
    checked: boolean
    onChange: (checked: boolean) => void
    multistate?: false
    onMultistateChange?: never
}

// A checkbox whose third, indeterminate state is an undefined `checked`.
interface MultistateCheckboxProps extends CheckboxBaseProps {
    checked: boolean | undefined
    multistate: true
    onMultistateChange: (checked: boolean | undefined) => void
    onChange?: never
}

// A labelled checkbox wearing the vanilla checkbox styling, optionally tri-state.
//
// If multistate is `true`, `onMultistateChange` will be called. Otherwise, `onChange`.
export const Checkbox = (props: TwoStateCheckboxProps | MultistateCheckboxProps) => {
    const isMultistate = props.multistate === true
    const isIndeterminate = isMultistate && props.checked === undefined
    const toggleState = isIndeterminate ? "undefined" : props.checked ? "checked" : "unchecked"
    const checkmarkState = isIndeterminate ? "undefined" : props.checked ? "checked" : ""

    const select = () => {
        if (props.multistate === true) {
            props.onMultistateChange(props.checked === undefined ? false : props.checked ? undefined : true)
            return
        }

        props.onChange(!props.checked)
    }

    return (
        <Tooltip tooltip={props.tooltip}>
            <div
                className={props.className}
                style={{ display: "flex", alignItems: "center", cursor: "pointer", ...props.style }}
                onClick={select}
            >
                {props.label !== undefined && (
                    <span style={{ marginRight: "6rem" }}>{props.label}</span>
                )}

                <div
                    className={`${VT.checkbox.toggle} ${toggleState}`}
                    style={{ transform: "scale(0.75)", filter: "grayscale(1)" }}
                >
                    <div className={`${VT.checkbox.checkmark} ${checkmarkState}`} />
                </div>
            </div>
        </Tooltip>
    )
}

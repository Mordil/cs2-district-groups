import { CSSProperties } from "react"
import { VT } from "./vanilla"

export const Checkbox = (props: {
    checked: boolean
    onChange: (checked: boolean) => void
    label: string
    className?: string
    style?: CSSProperties
}) => (
    <div
        className={props.className}
        style={{ display: "flex", alignItems: "center", cursor: "pointer", ...props.style }}
        onClick={() => props.onChange(!props.checked)}
    >
        <span style={{ marginRight: "6rem" }}>{props.label}</span>
        <div
            className={`${VT.checkbox.toggle} ${props.checked ? "checked" : "unchecked"}`}
            style={{ transform: "scale(0.75)", filter: "grayscale(1)" }}
        >
            <div className={`${VT.checkbox.checkmark} ${props.checked ? "checked" : ""}`} />
        </div>
    </div>
)

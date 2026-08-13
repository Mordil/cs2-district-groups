import { getModule } from "cs2/modding"
import { CSSProperties } from "react"

const checkboxClasses: any = getModule(
    "game-ui/common/input/toggle/checkbox/checkbox.module.scss",
    "classes"
)

export const Checkbox = (props: {
    checked: boolean
    onChange: (checked: boolean) => void
    label: string
    style?: CSSProperties
}) => (
    <div
        style={{ display: "flex", alignItems: "center", cursor: "pointer", ...props.style }}
        onClick={() => props.onChange(!props.checked)}
    >
        <span style={{ marginRight: "6rem" }}>{props.label}</span>
        <div
            className={`${checkboxClasses.toggle} ${props.checked ? "checked" : "unchecked"}`}
            style={{ transform: "scale(0.75)", filter: "grayscale(1)" }}
        >
            <div className={`${checkboxClasses.checkmark} ${props.checked ? "checked" : ""}`} />
        </div>
    </div>
)

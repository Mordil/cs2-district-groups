import { CSSProperties, ReactNode } from "react"

import { Color } from "cs2/bindings"
import { Tooltip } from "cs2/ui"

import { VC, VF } from "./vanilla"

// The game's own color swatch + click-to-open picker, wired to a Color value.
export const ColorPicker = (props: {
    value: Color
    onChange: (color: Color) => void
    tooltip?: ReactNode
    className?: string
    style?: CSSProperties
}) => (
    <Tooltip tooltip={props.tooltip}>
        <VC.ColorField
            className={props.className}
            style={props.style}
            value={props.value}
            focusKey={VF.FOCUS_DISABLED}
            // Group colors are always fully opaque - clamp here regardless of what the
            // picker's own alpha handling does, so a translucent pick can never persist.
            onChange={(color: Color) => props.onChange({ ...color, a: 1 })}
            alpha={false}
        />
    </Tooltip>
)

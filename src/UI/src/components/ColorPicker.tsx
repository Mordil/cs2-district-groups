import { Color } from "cs2/bindings"
import { getModule } from "cs2/modding"
import { Tooltip } from "cs2/ui"
import { CSSProperties, ReactNode } from "react"

// Not exported from cs2/ui (unlike Dropdown), so pulled from the game's own
// module registry - the same technique TypePicker.tsx uses for DropdownItem.
const ColorField: any = getModule(
    "game-ui/common/input/color-picker/color-field/color-field.tsx",
    "ColorField"
)
const FOCUS_DISABLED: any = getModule("game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED")

// The game's own color swatch + click-to-open picker, wired to a Color value.
export const ColorPicker = (props: {
    value: Color
    onChange: (color: Color) => void
    tooltip?: ReactNode
    className?: string
    style?: CSSProperties
}) => (
    <Tooltip tooltip={props.tooltip}>
        <ColorField
            className={props.className}
            style={props.style}
            value={props.value}
            focusKey={FOCUS_DISABLED}
            // Group colors are always fully opaque - clamp here regardless of what the
            // picker's own alpha handling does, so a translucent pick can never persist.
            onChange={(color: Color) => props.onChange({ ...color, a: 1 })}
            alpha={false}
        />
    </Tooltip>
)

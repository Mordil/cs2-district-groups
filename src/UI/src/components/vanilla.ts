import { getModule } from "cs2/modding"

// Central registry of vanilla game UI pieces pulled out of the game's own
// module registry - components/classnames/focus keys that aren't exported
// from the public cs2/ui package. Consolidating the getModule calls here
// means a game update only needs its paths fixed in one place.

// Vanilla Components
export const VC = {
    DropdownItem: getModule(
        "game-ui/common/input/dropdown/items/dropdown-item.tsx",
        "DropdownItem"
    ) as any,
    ColorField: getModule(
        "game-ui/common/input/color-picker/color-field/color-field.tsx",
        "ColorField"
    ) as any,
    InfoSection: getModule(
        "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
        "InfoSection"
    ) as any,
    InfoRow: getModule(
        "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
        "InfoRow"
    ) as any,
}

// Vanilla Themes (real CSS-module classname maps compiled by the game)
export const VT = {
    checkbox: getModule(
        "game-ui/common/input/toggle/checkbox/checkbox.module.scss",
        "classes"
    ) as any,
    editorDropdown: getModule(
        "game-ui/editor/themes/editor-dropdown.module.scss",
        "classes"
    ) as any,
}

// Vanilla Focus keys
export const VF = {
    FOCUS_DISABLED: getModule("game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED") as any,
}

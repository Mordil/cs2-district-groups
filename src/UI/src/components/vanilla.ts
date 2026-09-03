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
    IconButton: getModule("game-ui/common/input/button/icon-button.tsx", "IconButton") as any,
    InfoLink: getModule(
        "game-ui/game/components/selected-info-panel/shared-components/info-link/info-link.tsx",
        "InfoLink"
    ) as any,
    TabBar: getModule("game-ui/common/tabs/tabs.tsx", "TabBar") as any,
    Tab: getModule("game-ui/common/tabs/tabs.tsx", "Tab") as any,
    TabNav: getModule("game-ui/common/tabs/tabs.tsx", "TabNav") as any,
    TintedIcon: getModule("game-ui/common/image/tinted-icon.tsx", "TintedIcon") as any,
}

const iconButtonClasses = getModule(
    "game-ui/common/input/button/icon-button.module.scss",
    "classes"
) as any
const roundHighlightButtonClasses = getModule(
    "game-ui/common/input/button/themes/round-highlight-button.module.scss",
    "classes"
) as any

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
    sectionPrimaryButton: getModule(
        "game-ui/game/components/selected-info-panel/shared-components/primary-button/section-primary-button.module.scss",
        "classes"
    ) as any,
    iconButton: iconButtonClasses,
    roundHighlightButton: roundHighlightButtonClasses,
    roundIconButton: {
        button: `${iconButtonClasses.button} ${roundHighlightButtonClasses.button}`,
        icon: iconButtonClasses.icon,
    },
    panel: getModule("game-ui/common/panel/panel.module.scss", "classes") as any,
    districtsSection: getModule(
        "game-ui/game/components/selected-info-panel/selected-info-sections/building-sections/districts-section/districts-section.module.scss",
        "classes"
    ) as any,
}

// Vanilla Focus keys
export const VF = {
    FOCUS_DISABLED: getModule("game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED") as any,
}

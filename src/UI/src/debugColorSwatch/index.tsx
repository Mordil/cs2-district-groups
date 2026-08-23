import { Button, Portal, Scrollable } from "cs2/ui"
import { useState } from "react"
import { kIconStylePaths } from "../constants"
import { buttonVariantSamples } from "./buttonVariants"
import { colorTokenGroups } from "./colorTokens"
import css from "./index.module.scss"

// Dev-only visual reference for the vanilla CSS color variables cataloged in
// the "reference_cs2_vanilla_css_variables" memory - lets us eyeball how each
// token actually renders in-game instead of guessing from a hex value.
export const ColorSwatchDebugPanel = () => {
    const [open, setOpen] = useState(false)

    // The GameTopRight dock this mounts under animates itself via a CSS
    // transform, which makes any `position: fixed` descendant position
    // relative to THAT box instead of the viewport (the CSS spec's containing
    // block rule) - same reason GroupSearchFlyout has to render through a
    // Portal rather than fixed-positioning directly in place.
    return (
        <Portal>
            <button className={css.toggle} onClick={() => setOpen(!open)}>
                {open ? "Hide" : "Show"} CSS vars
            </button>
            {open && (
                <div className={css.panel}>
                    <Scrollable vertical={true} className={css.list}>
                        <div className={css.groupHeader}>Button variants</div>
                        {buttonVariantSamples.map(({ variant, label, selected }) => (
                            <div key={label} className={css.row}>
                                <Button
                                    variant={variant}
                                    src={kIconStylePaths[0]}
                                    selected={selected}
                                    onSelect={() => {}}
                                >
                                    {label}
                                </Button>
                            </div>
                        ))}
                        {colorTokenGroups.map((group) => (
                            <div key={group.label}>
                                <div className={css.groupHeader}>{group.label}</div>
                                {group.tokens.map((token) => (
                                    <div key={token} className={css.row}>
                                        <span className={css.swatch}>
                                            <span
                                                className={css.swatchFill}
                                                style={{ backgroundColor: `var(${token})` }}
                                            />
                                        </span>
                                        <span className={css.name}>{token}</span>
                                    </div>
                                ))}
                            </div>
                        ))}
                    </Scrollable>
                </div>
            )}
        </Portal>
    )
}

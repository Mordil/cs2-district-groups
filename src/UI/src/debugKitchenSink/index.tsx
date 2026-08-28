import { useValue } from "cs2/api"
import {
    Button,
    ConfirmationDialog,
    DialogStack,
    Dropdown,
    DropdownToggle,
    FormattedParagraphs,
    Icon,
    Portal,
    Scrollable,
    Tooltip,
} from "cs2/ui"
import { useContext, useState } from "react"
import { VC, VT } from "../components/vanilla"
import { glyphIconSrc } from "../components/icons"
import { isDebugBuild$, markdownRenderer } from "../shared"
import { buttonVariantSamples } from "./buttonVariants"
import { colorTokenGroups } from "./colorTokens"
import css from "./index.module.scss"
import { fontSizeTokens, radiusTokens } from "./otherTokens"

const kDropdownLabels = ["Option A", "Option B", "Option C"]

// Dev-only showcase of every reusable vanilla piece (cs2/ui components +
// design tokens) cataloged in the "reference_cs2_native_ui_components" and
// "reference_cs2_vanilla_css_variables" memories - lets us eyeball what's
// actually available before reaching for a custom-styled element. Gated on
// isDebugBuild$ (DistrictGroupsUISystem.IsDebugBuild, a real C# `#if DEBUG`
// check) so it never renders in a shipped Release build.
export const KitchenSinkDebugPanel = () => {
    const isDebugBuild = useValue(isDebugBuild$)
    const [open, setOpen] = useState(false)
    const [dropdownValue, setDropdownValue] = useState(0)
    const dialogStack = useContext(DialogStack)

    if (!isDebugBuild) {
        return null
    }

    const showDemoDialog = () => {
        dialogStack.showDialog(
            <ConfirmationDialog
                title="Kitchen sink demo"
                message="This is cs2/ui's ConfirmationDialog, opened via useContext(DialogStack)."
                confirm="OK"
                cancel="Cancel"
                onConfirm={() => dialogStack.closeAll()}
                onCancel={() => dialogStack.closeAll()}
            />
        )
    }

    return (
        <Portal>
            <button className={css.toggle} onClick={() => setOpen(!open)}>
                {open ? "Hide" : "Show"} kitchen sink
            </button>
            {open && (
                <div className={css.panel}>
                    <Scrollable vertical={true} className={css.list}>
                        <div className={css.groupHeader}>Button variants</div>
                        {buttonVariantSamples.map(({ variant, label, selected }) => (
                            <div key={label} className={css.row}>
                                <Button
                                    variant={variant}
                                    src={glyphIconSrc("ThickStrokeArrowRight")}
                                    selected={selected}
                                    onSelect={() => {}}
                                >
                                    {label}
                                </Button>
                            </div>
                        ))}

                        <div className={css.groupHeader}>Icon (tinted vs. untinted)</div>
                        <div className={css.row}>
                            <Icon tinted={true} src={glyphIconSrc("ThickStrokeArrowRight")} />
                            <Icon tinted={false} src={glyphIconSrc("ThickStrokeArrowRight")} />
                        </div>

                        <div className={css.groupHeader}>Dropdown</div>
                        <div className={css.row}>
                            <Dropdown
                                theme={VT.editorDropdown}
                                content={kDropdownLabels.map((label, i) => (
                                    <VC.DropdownItem
                                        key={i}
                                        value={i}
                                        className={VT.editorDropdown.dropdownItem}
                                        selected={i === dropdownValue}
                                        closeOnSelect={true}
                                        onChange={() => setDropdownValue(i)}
                                    >
                                        <div>{label}</div>
                                    </VC.DropdownItem>
                                ))}
                            >
                                <DropdownToggle
                                    disabled={false}
                                    openIconComponent={<></>}
                                    closeIconComponent={<></>}
                                    className={css.dropdownToggle}
                                >
                                    <div>{kDropdownLabels[dropdownValue]}</div>
                                </DropdownToggle>
                            </Dropdown>
                        </div>

                        <div className={css.groupHeader}>Tooltip</div>
                        <div className={css.row}>
                            <Tooltip tooltip="This is a cs2/ui Tooltip.">
                                <span className={css.tooltipTarget}>Hover me</span>
                            </Tooltip>
                        </div>

                        <div className={css.groupHeader}>Formatted text (FormattedParagraphs + MarkdownRenderer)</div>
                        <div className={css.row}>
                            <FormattedParagraphs
                                renderer={markdownRenderer}
                                text={["Supports **bold**, *italic*, and multiple paragraphs."]}
                            />
                        </div>

                        <div className={css.groupHeader}>Panel section / row (InfoSection / InfoRow)</div>
                        <div className={css.row}>
                            <VC.InfoSection disableFocus={true}>
                                <VC.InfoRow left="Label" right="Value" uppercase={true} disableFocus={true} />
                            </VC.InfoSection>
                        </div>

                        <div className={css.groupHeader}>Confirmation dialog</div>
                        <div className={css.row}>
                            <Button variant="flat" onSelect={showDemoDialog}>
                                Show confirmation dialog
                            </Button>
                        </div>

                        <div className={css.groupHeader}>Typography scale</div>
                        {fontSizeTokens.map((token) => (
                            <div key={token} className={css.row}>
                                <span className={css.fontSample} style={{ fontSize: `var(${token})` }}>
                                    Ag
                                </span>
                                <span className={css.name}>{token}</span>
                            </div>
                        ))}

                        <div className={css.groupHeader}>Radius</div>
                        {radiusTokens.map((token) => (
                            <div key={token} className={css.row}>
                                <span className={css.radiusBox} style={{ borderRadius: `var(${token})` }} />
                                <span className={css.name}>{token}</span>
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

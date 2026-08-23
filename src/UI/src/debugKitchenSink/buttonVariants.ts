// All variants cs2/ui's Button supports (see ButtonProps$1 in src/UI/types/ui.d.ts).
export interface ButtonVariantSample {
    variant: "flat" | "primary" | "round" | "menu" | "icon" | "floating" | "default"
    label: string
    selected?: boolean
}

export const buttonVariantSamples: ButtonVariantSample[] = [
    { variant: "default", label: "Default" },
    { variant: "flat", label: "Flat" },
    { variant: "flat", label: "Flat (selected)", selected: true },
    { variant: "primary", label: "Primary" },
    { variant: "primary", label: "Primary (selected)", selected: true },
    { variant: "menu", label: "Menu" },
    { variant: "round", label: "Round" },
    { variant: "icon", label: "Icon" },
    { variant: "floating", label: "Floating" },
]

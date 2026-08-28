/*
    Mod assets are served at coui://<mod key>/<path>

    Game UI assets are in Media/Game/<path>
*/

const kModIcons = "coui://districtgroups/"
const kGameIcons = "Media/Game/Icons/"
const kGameGlyphs = "Media/Glyphs/"

export interface IconProps {
    name: string
    size?: string
    width?: string
    height?: string
}

function icon(source: string, props: IconProps) {
    return <img
        src={source + props.name + ".svg"}
        style={{ width: props.size ?? props.width ?? "20rem", height: props.size ?? props.height ?? "20rem" }}
    />
}

export const GameIcon = (props: IconProps) => icon(kGameIcons, props)
export const GlyphIcon = (props: IconProps) => icon(kGameGlyphs, props)
export const ModIcon = (props: IconProps) => icon(kModIcons, props)

// For consumers that need the raw coui:// path instead of a rendered <img>
export const gameIconSrc = (name: string): string => `${kGameIcons}${name}.svg`
export const glyphIconSrc = (name: string): string => `${kGameGlyphs}${name}.svg`
export const modIconSrc = (name: string): string => `${kModIcons}${name}.svg`

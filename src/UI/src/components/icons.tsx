/*
    Mod assets are served at coui://<mod key>/<path>

    Game UI assets are in Media/Game/<path>
*/

const kUilStandard = "coui://uil/Standard/"
const kModIcons = "coui://districtgroups/"
const kGameIcons = "Media/Game/Icons/"

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

export const UilIcon = (props: IconProps) => icon(kUilStandard, props)
export const GameIcon = (props: IconProps) => icon(kGameIcons, props)

// For consumers that need the raw coui:// path instead of a rendered <img>
export const uilIconSrc = (name: string): string => `${kUilStandard}${name}.svg`

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
        style={{ width: props.size ?? props.width ?? "16rem", height: props.size ?? props.height ?? "16rem" }}
    />
}

export const UilIcon = (props: IconProps) => icon(kUilStandard, props)
export const ModIcon = (props: IconProps) => icon(kModIcons, props)
export const GameIcon = (props: IconProps) => icon(kGameIcons, props)

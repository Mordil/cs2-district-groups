// Icons from the Unified Icon Library mod (PDX Mods id 74417, declared as a
// dependency in Properties/PublishConfiguration.xml). Served by the game at
// coui://uil/ when UIL is installed.
const kUilStandard = "coui://uil/Standard/";

export const UilIcon = (props: { name: string; size?: string; width?: string; height?: string; }) => (
    <img
        src={kUilStandard + props.name + ".svg"}
        style={{ width: props.size ?? props.width ?? "16rem", height: props.size ?? props.height ?? "16rem" }}
    />
);

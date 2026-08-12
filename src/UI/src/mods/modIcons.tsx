// This mod's own icons, shipped in Icons/ and served over the coui:// host that
// Mod.OnLoad registers (see kIconHost in src/Code/Mod.cs - keep the two in sync).
// Unlike UIL icons these need no dependency, but they do need the DeployIcons
// csproj target to have copied Icons/ next to the DLL.
const kModIcons = "coui://districtgroups/";

export const ModIcon = (props: { name: string; size?: string; width?: string; height?: string; }) => (
    <img
        src={kModIcons + props.name + ".svg"}
        style={{ width: props.size ?? props.width ?? "16rem", height: props.size ?? props.height ?? "16rem" }}
    />
);

import { gameIconSrc, modIconSrc } from "./components/icons"
import { VanillaLocale, useTranslation, useVanillaLabel } from "./utils/locale"

export const kAssetPath = 'coui://districtgroups/'

// index 0 is the color icon, index 1 is monochrome.
export const kIconStylePaths = [
    `${kAssetPath}mod-icon-color.svg`,
    `${kAssetPath}mod-icon-mono.svg`,
]

// The constant that any UI panels that should be offset from the top of the screen, as REM units.
export const kUITopOffset = 60;

// GroupServiceType.Generic - the "no type selected" sentinel for the type filter.
export const kGenericType = 0

// AssignedBuilding.efficiency for a building the game has no efficiency to report for.
export const kUnknownEfficiency = -1

// Any district or group figure the sweep had nothing to work out, which reads as kNoValue.
export const kNoThreshold = -1

// The size of the main panel, in rem CSS units
export const kPanelWidth = 490

// How wide the group info panel is allowed to grow to fit longer content, in rem CSS units
export const kGroupInfoPanelMaxWidth = kPanelWidth * 1.25

// Stands in for a district stat that is unreported.
export const kNoValue = "—"

// Indexed by GroupServiceType - order must match the C# enum.
// 
// The "Civic"(Generic) entry is the mod's own icon;
// every other entry reuses the game's own icon for that service.
export const kTypeIcons: string[] = [
    modIconSrc("civic"),
    gameIconSrc("Police"),
    gameIconSrc("FireSafety"),
    gameIconSrc("Healthcare"),
    gameIconSrc("Deathcare"),
    gameIconSrc("Garbage"),
    gameIconSrc("Education"),
    gameIconSrc("Education"),
    gameIconSrc("Education"),
    gameIconSrc("Education"),
    gameIconSrc("PostService"),
]

// Indexed by GroupServiceType (src/Code/DistrictGroupComponents.cs) - order
// must match the C# enum.
export const useTypeLabels = (): string[] => {
    const t = useTranslation()
    // Reuses the game's own Info View option label rather than a mod-owned translation, so this entry
    // stays in whatever wording the player's language already knows and never drifts from it.
    const fireAndRescue = useVanillaLabel(VanillaLocale.fireAndRescueType)
    return [
        t("typeGeneric"),
        t("typePolice"),
        fireAndRescue,
        t("typeHealthcare"),
        t("typeDeathcare"),
        t("typeGarbage"),
        t("typeEducationElementary"),
        t("typeEducationHighSchool"),
        t("typeEducationCollege"),
        t("typeEducationUniversity"),
        t("typePost"),
    ]
}

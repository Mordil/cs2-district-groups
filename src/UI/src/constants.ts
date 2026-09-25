import { gameIconSrc, modIconSrc } from "./components/icons"
import { VanillaLocale, useTranslation, useVanillaLabels } from "./utils/locale"

export const kAssetPath = 'coui://districtgroups/'

// index 0 is the color icon, index 1 is monochrome.
export const kIconStylePaths = [
    `${kAssetPath}mod-icon-color.svg`,
    `${kAssetPath}mod-icon-mono.svg`,
]

// The constant that any UI panels that should be offset from the top of the screen, as REM units.
export const kUITopOffset = 60;

// GroupServiceType.Generic (Civic) - the type filter's default selection, and the fallback entry of
// every per-type table under groupStats.
export const kGenericType = 0

// Any figure there is nothing to report for, which reads as kNoValueText.
export const kNoValue = -1

// The size of the main panel, in rem CSS units
export const kPanelWidth = 490

// How wide the group info panel is allowed to grow to fit longer content, in rem CSS units
export const kGroupInfoPanelMaxWidth = kPanelWidth * 1.3

// Stands in for a district stat that is unreported.
export const kNoValueText = "—"

// Indexed by GroupServiceType - order must match the C# enum.
//
// The "Civic"(Generic) entry is the mod's own icon;
// every other entry reuses the game's own icon for that service.
//
// The Administration entry is detected per-building only and is never a group's own type, so it is
// past the range the group type picker offers as an option.
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
    gameIconSrc("Administration"),
]

// Indexed by GroupServiceType (src/Code/DistrictGroupComponents.cs) - order
// must match the C# enum.
export const useTypeLabels = (): string[] => {
    const t = useTranslation()
    /*
        Every entry but Civic reuses the game's own name for that service, so each one stays in
        whatever wording the player's language already knows and never drifts from it. Civic is the
        mod's own concept and has no game equivalent to borrow.
    */
    const serviceTypes = useVanillaLabels([
        VanillaLocale.policeType,
        VanillaLocale.fireAndRescueType,
        VanillaLocale.healthcareType,
        VanillaLocale.deathcareType,
        VanillaLocale.garbageType,
        VanillaLocale.elementarySchoolType,
        VanillaLocale.highSchoolType,
        VanillaLocale.collegeType,
        VanillaLocale.universityType,
        VanillaLocale.postType,
    ])

    return [t("typeGeneric"), ...serviceTypes]
}

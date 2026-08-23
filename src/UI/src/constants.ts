import { useTranslation } from "./locale"

export const kAssetPath = 'coui://districtgroups/'

// index 0 is the color icon, index 1 is monochrome.
export const kIconStylePaths = [
    `${kAssetPath}mod-icon-color.svg`,
    `${kAssetPath}mod-icon-mono.svg`,
]

// The constant that any UI panels that should be offset from the top of the screen, as REM units.
export const kUITopOffset = 60;

// Indexed by GroupServiceType (src/Code/DistrictGroupComponents.cs) - order
// must match the C# enum.
export const useTypeLabels = (): string[] => {
    const t = useTranslation()
    return [
        t("typeGeneric"),
        t("typePolice"),
        t("typeFire"),
        t("typeHealthcare"),
        t("typeDeathcare"),
        t("typeGarbage"),
        t("typeEducationElementary"),
        t("typeEducationHighSchool"),
        t("typeEducationCollege"),
        t("typeEducationUniversity"),
        t("typePost"),
        t("typeParks"),
        t("typeWelfare"),
    ]
}

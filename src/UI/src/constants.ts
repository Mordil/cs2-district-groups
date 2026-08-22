import { useTranslation } from "./locale"

export const kAssetPath = 'coui://districtgroups/'

// Indexed by ModIconStyle (src/Code/DistrictGroupTypes.cs) - order must match the C# enum.
export const kIconStylePaths = [
    `${kAssetPath}mod-icon-color.svg`,
    `${kAssetPath}mod-icon-mono.svg`,
]

export const kIconStyleMonochrome = 1

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

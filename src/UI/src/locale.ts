import { useLocalization } from "cs2/l10n"

// Locale-id builder: keeps ids consistent with the C# dictionary sources
// under src/Code/UILocale.cs, which must define matching keys - the
// "DistrictGroups.UI[Key]" scheme matches the game's own locale id style.
const id = (key: string) => `DistrictGroups.UI[${key}]`

export const kLocale = {
    panelTitle: id("PanelTitle"),
    newGroupButton: id("NewGroupButton"),
    newGroupButtonTooltip: id("NewGroupButtonTooltip"),
    newGroupDefaultName: id("NewGroupDefaultName"),
    filterTooltipLine1: id("FilterTooltipLine1"),
    filterTooltipLine2: id("FilterTooltipLine2"),
    allGroupsLabel: id("AllGroupsLabel"),
    noGroupsYet: id("NoGroupsYet"),
    noGroupsMatchFilter: id("NoGroupsMatchFilter"),
    displayDistrictAreasLabel: id("DisplayDistrictAreasLabel"),
    showGroupOverlayLabel: id("ShowGroupOverlayLabel"),

    deleteGroupTooltipLine1: id("DeleteGroupTooltipLine1"),
    deleteGroupTooltipLine2: id("DeleteGroupTooltipLine2"),
    typePickerTooltipLine1: id("TypePickerTooltipLine1"),
    typePickerTooltipLine2: id("TypePickerTooltipLine2"),
    typePickerTooltipLine3: id("TypePickerTooltipLine3"),
    deleteGroupConfirmMessage: id("DeleteGroupConfirmMessage"),
    deleteGroupDialogTitle: id("DeleteGroupDialogTitle"),
    deleteGroupConfirm: id("DeleteGroupConfirm"),
    deleteGroupCancel: id("DeleteGroupCancel"),
    removeMemberTooltip: id("RemoveMemberTooltip"),
    selectDistrictsButton: id("SelectDistrictsButton"),
    groupColorTooltip: id("GroupColorTooltip"),

    toggleTooltipTitle: id("ToggleTooltipTitle"),
    toggleTooltipBody: id("ToggleTooltipBody"),

    sectionTooltipLine1: id("SectionTooltipLine1"),
    sectionTooltipLine2: id("SectionTooltipLine2"),
    sectionTooltipLine3: id("SectionTooltipLine3"),
    sectionTooltipLine4: id("SectionTooltipLine4"),
    sectionLabel: id("SectionLabel"),
    unassignOption: id("UnassignOption"),
    unassignedLabel: id("UnassignedLabel"),

    typeGeneric: id("TypeGeneric"),
    typePolice: id("TypePolice"),
    typeFire: id("TypeFire"),
    typeHealthcare: id("TypeHealthcare"),
    typeDeathcare: id("TypeDeathcare"),
    typeGarbage: id("TypeGarbage"),
    typeEducationElementary: id("TypeEducationElementary"),
    typeEducationHighSchool: id("TypeEducationHighSchool"),
    typeEducationCollege: id("TypeEducationCollege"),
    typeEducationUniversity: id("TypeEducationUniversity"),
    typePost: id("TypePost"),
    typeParks: id("TypeParks"),
    typeWelfare: id("TypeWelfare"),
} as const

// English fallbacks - shown as-is until a locale source (en-US at minimum,
// see src/Code/UILocale.cs) registers a translation for the id, and also
// used to fill in translate()'s own fallback parameter.
const kFallback: Record<keyof typeof kLocale, string> = {
    panelTitle: "District Groups",
    newGroupButton: "New Group",
    newGroupButtonTooltip: "Adds a new group with no member districts.",
    newGroupDefaultName: "New Group {NUMBER}",
    filterTooltipLine1: "Filter the list of groups by their **type**.",
    filterTooltipLine2: "If **All Groups** is selected, then all groups will be listed.",
    allGroupsLabel: "All Groups",
    noGroupsYet: "No groups yet. Create one above.",
    noGroupsMatchFilter: "No groups match this filter.",
    displayDistrictAreasLabel: "Display District areas",
    showGroupOverlayLabel: "Show group overlay",

    deleteGroupTooltipLine1: "Permanently delete the group.",
    deleteGroupTooltipLine2: "Assigned buildings will lose their **operating districts**.",
    typePickerTooltipLine1: "Change the **type** of the group.",
    typePickerTooltipLine2: "**Generic** groups can be assigned to any service building.",
    typePickerTooltipLine3: "All other types are only available to matching service buildings.",
    deleteGroupConfirmMessage:
        '"{NAME}" is assigned to {COUNT} service building(s).\nAssigned service building(s) will serve the whole city again.',
    deleteGroupDialogTitle: "Delete District Group?",
    deleteGroupConfirm: "Delete group",
    deleteGroupCancel: "Keep group",
    removeMemberTooltip: "Remove the district from the group.",
    selectDistrictsButton: "Select Districts",
    groupColorTooltip: "Group Color",

    toggleTooltipTitle: "**DISTRICT GROUPS**",
    toggleTooltipBody:
        "Create groups of districts to assign to service buildings for self-managing of **operating districts**.",

    sectionTooltipLine1: "Service buildings can be assigned to a **district group**.",
    sectionTooltipLine2: "When assigned, the group will manage the **operating districts** for the building.",
    sectionTooltipLine3: "When unassigned, **operating districts** are managed manually.",
    sectionTooltipLine4: "NOTE: The Info Panel can take a few seconds to visually update after changing the assignment.",
    sectionLabel: "DISTRICT GROUP",
    unassignOption: "None (Unassign)",
    unassignedLabel: "Unassigned",

    typeGeneric: "Generic",
    typePolice: "Police",
    typeFire: "Fire",
    typeHealthcare: "Healthcare",
    typeDeathcare: "Deathcare",
    typeGarbage: "Garbage",
    typeEducationElementary: "Elementary School",
    typeEducationHighSchool: "High School",
    typeEducationCollege: "College",
    typeEducationUniversity: "University",
    typePost: "Post",
    typeParks: "Parks",
    typeWelfare: "Welfare",
}

type LocaleKey = keyof typeof kLocale

// translate() has no built-in {PLACEHOLDER} substitution (that's only wired
// up for the JSX <LocalizedString> component's `args`), so plain-string
// contexts here - tooltip paragraphs, dialog messages - interpolate by hand.
export const useTranslation = () => {
    const { translate } = useLocalization()
    return (key: LocaleKey, params?: Record<string, string | number>): string => {
        const fallback = kFallback[key]
        const template = translate(kLocale[key], fallback) ?? fallback
        if (!params) {
            return template
        }
        return Object.entries(params).reduce(
            (text, [name, value]) => text.split(`{${name.toUpperCase()}}`).join(String(value)),
            template
        )
    }
}

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
    allGroupsLabel: id("AllGroupsLabel"),
    noGroupsYet: id("NoGroupsYet"),
    noGroupsMatchFilter: id("NoGroupsMatchFilter"),
    displayDistrictAreasLabel: id("DisplayDistrictAreasLabel"),
    showGroupOverlayLabel: id("ShowGroupOverlayLabel"),
    showServiceBuildingsLabel: id("ShowServiceBuildingsLabel"),

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
    nameInputTooltip: id("NameInputTooltip"),
    metadataDistrictsTooltip: id("MetadataDistrictsTooltip"),
    metadataBuildingsTooltip: id("MetadataBuildingsTooltip"),
    metadataPopulationTooltip: id("MetadataPopulationTooltip"),

    toggleTooltipTitle: id("ToggleTooltipTitle"),
    toggleTooltipBody: id("ToggleTooltipBody"),

    sectionTooltipLine1: id("SectionTooltipLine1"),
    sectionTooltipLine2: id("SectionTooltipLine2"),
    sectionTooltipLine3: id("SectionTooltipLine3"),
    sectionTooltipLine4: id("SectionTooltipLine4"),
    sectionLabel: id("SectionLabel"),
    operatingDistrictsLabel: id("OperatingDistrictsLabel"),
    readOnlySectionTooltipLine1: id("ReadOnlySectionTooltipLine1"),
    readOnlySectionTooltipLine2: id("ReadOnlySectionTooltipLine2"),
    readOnlySectionTooltipLine3: id("ReadOnlySectionTooltipLine3"),
    unassignOption: id("UnassignOption"),
    unassignTooltipDisabled: id("UnassignTooltipDisabled"),
    unassignTooltipEnabled: id("UnassignTooltipEnabled"),
    unassignedLabel: id("UnassignedLabel"),
    groupSearchTitle: id("GroupSearchTitle"),
    searchGroupsPlaceholder: id("SearchGroupsPlaceholder"),
    noGroupsMatchSearch: id("NoGroupsMatchSearch"),
    noGroupsInSection: id("NoGroupsInSection"),

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
    allGroupsLabel: "All Groups",
    noGroupsYet: "No groups yet. Create one with the NEW GROUP button.",
    noGroupsMatchFilter: "No groups match this filter.",
    displayDistrictAreasLabel: "Display District areas",
    showGroupOverlayLabel: "Show group overlay",
    showServiceBuildingsLabel: "Show service buildings",

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
    nameInputTooltip: "Select to edit the name.",
    metadataDistrictsTooltip: "Districts",
    metadataBuildingsTooltip: "Assigned buildings",
    metadataPopulationTooltip: "Population",

    toggleTooltipTitle: "**DISTRICT GROUPS**",
    toggleTooltipBody:
        "Create groups of districts to assign to service buildings for self-managing of **operating districts**.",

    sectionTooltipLine1: "Service buildings can be assigned to a **district group**.",
    sectionTooltipLine2: "When assigned, the group will manage the **operating districts** for the building.",
    sectionTooltipLine3: "When unassigned, **operating districts** are managed manually.",
    sectionTooltipLine4: "NOTE: The Info Panel can take a few seconds to visually update after changing the assignment.",
    sectionLabel: "DISTRICT GROUP",
    operatingDistrictsLabel: "Operating Districts",
    readOnlySectionTooltipLine1: "This building is currently assigned to a district group.",
    readOnlySectionTooltipLine2:
        "**City district** assignments will be managed by the **assigned district group**.",
    readOnlySectionTooltipLine3:
        "If no **city district** is in the **district group**, this building will provide their services everywhere within their **operational radius**.",
    unassignOption: "Unassign",
    unassignTooltipDisabled: "No group is assigned.",
    unassignTooltipEnabled: "Removes the current group assignment.",
    unassignedLabel: "Unassigned",
    groupSearchTitle: "Select District Group",
    searchGroupsPlaceholder: "Search...",
    noGroupsMatchSearch: "No groups match your search.",
    noGroupsInSection: "No groups found.",

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

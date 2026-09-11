import { useLocalization } from "cs2/l10n"

// Locale-id builder: keeps ids consistent with the C# dictionary sources
// under src/Code/UILocale.cs, which must define matching keys - the
// "DistrictGroups.UI[Key]" scheme matches the game's own locale id style.
const id = (key: string) => `DistrictGroups.UI[${key}]`

export const kLocale = {
    panelTitle: id("PanelTitle"),
    groupsTabLabel: id("GroupsTabLabel"),
    assignmentsTabLabel: id("AssignmentsTabLabel"),
    selectTypeForAssignments: id("SelectTypeForAssignments"),
    noServiceBuildingsMatchFilter: id("NoServiceBuildingsMatchFilter"),
    hideAssignedBuildingsLabel: id("HideAssignedBuildingsLabel"),
    hideAssignedBuildingsTooltip: id("HideAssignedBuildingsTooltip"),
    newGroupButton: id("NewGroupButton"),
    newGroupButtonTooltip: id("NewGroupButtonTooltip"),
    filterTooltipLine1: id("FilterTooltipLine1"),
    filterTooltipAssignmentsLine1: id("FilterTooltipAssignmentsLine1"),
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
    removeBuildingTooltip: id("RemoveBuildingTooltip"),
    selectDistrictsButton: id("SelectDistrictsButton"),
    groupColorTooltip: id("GroupColorTooltip"),
    nameInputTooltip: id("NameInputTooltip"),
    overviewTabLabel: id("OverviewTabLabel"),
    buildingsTabLabel: id("BuildingsTabLabel"),
    policiesTabLabel: id("PoliciesTabLabel"),
    typeColumnLabel: id("TypeColumnLabel"),
    noDistrictsInGroup: id("NoDistrictsInGroup"),
    noBuildingsInGroup: id("NoBuildingsInGroup"),
    applyPolicyToGroupTooltip: id("ApplyPolicyToGroupTooltip"),
    clearPolicyFromGroupTooltip: id("ClearPolicyFromGroupTooltip"),
    mixedPolicyValueTooltip: id("MixedPolicyValueTooltip"),
    metadataDistrictsTooltip: id("MetadataDistrictsTooltip"),
    metadataBuildingsTooltip: id("MetadataBuildingsTooltip"),
    metadataPopulationTooltip: id("MetadataPopulationTooltip"),
    metadataHappinessTooltip: id("MetadataHappinessTooltip"),
    metadataWealthTooltip: id("MetadataWealthTooltip"),
    showOverlayAndBuildingsLabel: id("ShowOverlayAndBuildingsLabel"),

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
    groupsTabLabel: "Groups",
    assignmentsTabLabel: "Service Buildings",
    selectTypeForAssignments: "Select a service type to list its service buildings.",
    noServiceBuildingsMatchFilter: "No service buildings match this filter.",
    hideAssignedBuildingsLabel: "Hide assigned buildings",
    hideAssignedBuildingsTooltip: "Hides service buildings that are already assigned to a **district group**.",
    newGroupButton: "New Group",
    newGroupButtonTooltip: "Adds a new group with no member districts.",
    filterTooltipLine1: "Filter the list of groups by their **type**.",
    filterTooltipAssignmentsLine1: "Filter the list of service buildings by their **type**.",
    allGroupsLabel: "All Groups",
    noGroupsYet: "No groups yet. Create one with the NEW GROUP button.",
    noGroupsMatchFilter: "No groups match this filter.",
    displayDistrictAreasLabel: "Show all districts",
    showGroupOverlayLabel: "Show group overlay",
    showServiceBuildingsLabel: "Show service buildings",

    deleteGroupTooltipLine1: "Permanently delete the group.",
    deleteGroupTooltipLine2: "Assigned buildings will lose their **operating districts**.",
    typePickerTooltipLine1: "Change the **type** of the group.",
    typePickerTooltipLine2: "**Civic** groups can be assigned to any service building.",
    typePickerTooltipLine3: "All other types are only available to matching service buildings.",
    deleteGroupConfirmMessage:
        '"{NAME}" is assigned to {COUNT} service building(s).\nAssigned service building(s) will serve the whole city again.',
    deleteGroupDialogTitle: "Delete District Group?",
    deleteGroupConfirm: "Delete group",
    deleteGroupCancel: "Keep group",
    removeMemberTooltip: "Remove the district from the group.",
    removeBuildingTooltip: "Remove the building from the group.",
    selectDistrictsButton: "Select Districts",
    groupColorTooltip: "Group Color",
    nameInputTooltip: "Select to edit the name.",
    overviewTabLabel: "Overview",
    buildingsTabLabel: "Buildings",
    policiesTabLabel: "Policies",
    typeColumnLabel: "Type",
    noDistrictsInGroup: "There are no districts in this group. Add districts with the SELECT DISTRICTS button.",
    noBuildingsInGroup: "This group has no assigned buildings yet.",
    applyPolicyToGroupTooltip: "Applies the policy to all {COUNT} districts in this group.",
    clearPolicyFromGroupTooltip: "Removes the policy from all {COUNT} districts in this group.",
    mixedPolicyValueTooltip: "Districts in this group are set to different values.",
    metadataDistrictsTooltip: "Districts",
    metadataBuildingsTooltip: "Assigned buildings",
    metadataPopulationTooltip: "Population",
    metadataHappinessTooltip: "Average happiness",
    metadataWealthTooltip: "Average wealth",
    showOverlayAndBuildingsLabel: "Show overlay and buildings",

    toggleTooltipTitle: "**DISTRICT GROUPS**",
    toggleTooltipBody:
        "Create groups of districts to assign to service buildings for self-managing of **operating districts**.",

    sectionTooltipLine1: "Service buildings can be assigned to a **district group**.",
    sectionTooltipLine2: "When assigned, the group will manage the **operating districts** for the building.",
    sectionTooltipLine3: "When unassigned, **operating districts** are controlled manually.",
    sectionTooltipLine4: "NOTE: The Info Panel can take a few seconds to visually update after changing the assignment.",
    sectionLabel: "DISTRICT GROUP",
    operatingDistrictsLabel: "Operating Districts",
    readOnlySectionTooltipLine1: "This building is currently assigned to a district group.",
    readOnlySectionTooltipLine2:
        "**City district** assignments are controlled by the **assigned district group**.",
    readOnlySectionTooltipLine3:
        "If the **district group** does not have **city districts**, this building will provide services everywhere within their **operational radius**.",
    unassignOption: "Unassign",
    unassignTooltipDisabled: "No group is assigned.",
    unassignTooltipEnabled: "Removes the current group assignment.",
    unassignedLabel: "Unassigned",
    groupSearchTitle: "Select District Group",
    searchGroupsPlaceholder: "Search...",
    noGroupsMatchSearch: "No groups match your search.",
    noGroupsInSection: "No groups found.",

    typeGeneric: "Civic",
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

// A display string the game itself ships, with the English text to fall back on.
export interface VanillaLabel {
    id: string
    fallback: string
}

// Locale ids owned by the game rather than the mod.
export const VanillaLocale = {
    details: { id: "SelectedInfoPanel.DETAILS", fallback: "View Details" },
    total: { id: "TransportInfoPanel.TOTAL", fallback: "Total" },
    districtsColumn: { id: "Glossary.SECTION_TITLE[Districts]", fallback: "Districts" },
    populationColumn: { id: "Glossary.SECTION_TITLE[Population]", fallback: "Population" },
    happinessColumn: { id: "Glossary.SECTION_TITLE[Happiness]", fallback: "Happiness" },
    wealthColumn: { id: "StatisticsPanel.STAT_TITLE[Wealth]", fallback: "Wealth" },
    buildingsColumn: { id: "EconomyPanel.SERVICES_TITLE_BUILDINGS", fallback: "Buildings" },
    efficiencyColumn: { id: "SelectedInfoPanel.EFFICIENCY", fallback: "Efficiency" },
    focusTooltip: {
        id: "SelectedInfoPanel.TOOLTIP[ActionsSectionFocus]",
        fallback: "Center the camera on the selected item.",
    },
}

/*
    The band names the game shows for citizen happiness and household wealth, in the order of the
    C# enums whose ordinals the bindings send (Game.Citizens.CitizenHappiness and
    Game.UI.InGame.HouseholdWealthKey).
*/
const kHappinessThresholds: VanillaLabel[] = [
    { id: "SelectedInfoPanel.CITIZEN_HAPPINESS_TITLE[Depressed]", fallback: "Unhappy" },
    { id: "SelectedInfoPanel.CITIZEN_HAPPINESS_TITLE[Sad]", fallback: "Sad" },
    { id: "SelectedInfoPanel.CITIZEN_HAPPINESS_TITLE[Neutral]", fallback: "Neutral" },
    { id: "SelectedInfoPanel.CITIZEN_HAPPINESS_TITLE[Content]", fallback: "Content" },
    { id: "SelectedInfoPanel.CITIZEN_HAPPINESS_TITLE[Happy]", fallback: "Happy" },
]

const kWealthThresholds: VanillaLabel[] = [
    { id: "WealthInfoPanel.AVERAGE_WEALTH_KEY[Wretched]", fallback: "Wretched" },
    { id: "WealthInfoPanel.AVERAGE_WEALTH_KEY[Poor]", fallback: "Poor" },
    { id: "WealthInfoPanel.AVERAGE_WEALTH_KEY[Modest]", fallback: "Modest" },
    { id: "WealthInfoPanel.AVERAGE_WEALTH_KEY[Comfortable]", fallback: "Comfortable" },
    { id: "WealthInfoPanel.AVERAGE_WEALTH_KEY[Wealthy]", fallback: "Wealthy" },
]

// The label for a happiness band, or null for the kNoThreshold ordinal.
export const happinessThreshold = (ordinal: number): VanillaLabel | null =>
    kHappinessThresholds[ordinal] ?? null

// The label for a household-wealth band, or null for the kNoThreshold ordinal.
export const wealthThreshold = (ordinal: number): VanillaLabel | null =>
    kWealthThresholds[ordinal] ?? null

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

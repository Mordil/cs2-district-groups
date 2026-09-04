using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            /*
                Key strings for the custom mod UI must match locale.ts's id() exactly
            */

            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "District Groups" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Main" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Overlay" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Developer" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Default" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Troubleshooting" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Border width" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Width of the district boundary lines drawn on the overlay." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "Border transparency" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "Transparency of the district boundary lines drawn on the overlay.\n\n0% is fully opaque, 100% is fully transparent." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Scene desaturation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "How much the rest of the scene is desaturated while the group overlay is visible.\n\n0% leaves the scene untouched, 100% is grayscale." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Fill saturation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "How saturated the group overlay fill areas are.\n\n100% is the group's full color; lower values fade towards gray." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Use Fill transparency" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Enable transparency for the group overlay fill areas.\n\nWhen disabled, the overlay will display a solid, fully opaque color that masks all other visuals." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "Show group labels" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "When enabled, group names will be displayed with the overlay on the map.\n\nThis may affect performance." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable debug logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Writes verbose Debug-level entries to the mod's log file.\n\nThis may affect performance." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Log debug mod data" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Writes all mod state (groups, service buildings, etc.) to the mod's log file.\n\nInclude the the log file when filing a bug report." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "File a bug" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Logs debug data, then opens the mod's GitHub issue tracker in your browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Refresh rate" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "The number of seconds to wait between UI updates for aggregate district information.\n\nUpdating frequently may negatively impact performance." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Reset all settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Resets all mod settings back to their default values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "All mod settings will revert to their default values.\r\nDo you want to proceed?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "The installed version of the mod.\n\nInclude this when filing a bug report." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Release channel" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Remove mod data" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Deletes every district group, service building assignment, and overlay asset the mod has added to the current save." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "All district groups, service building assignments, and overlay data will be permanently deleted from this save.\r\nThis cannot be undone.\r\nDo you want to proceed?" },

                { LocalizationKey.PanelTitle, "District Groups" },
                { LocalizationKey.GroupsTabLabel, "Groups" },
                { LocalizationKey.AssignmentsTabLabel, "Service Buildings" },
                { LocalizationKey.SelectTypeForAssignments, "Select a service type to list its service buildings." },
                { LocalizationKey.NoServiceBuildingsMatchFilter, "No service buildings match this filter." },
                { LocalizationKey.HideAssignedBuildingsLabel, "Hide assigned buildings" },
                { LocalizationKey.HideAssignedBuildingsTooltip, "Hides service buildings that are already assigned to a **district group**." },
                { LocalizationKey.NewGroupButton, "New Group" },
                { LocalizationKey.NewGroupButtonTooltip, "Adds a new group with no member districts." },
                { LocalizationKey.NewGroupDefaultName, "New Group {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filter the list of groups by their **type**." },
                { LocalizationKey.FilterTooltipAssignmentsLine1, "Filter the list of service buildings by their **type**." },
                { LocalizationKey.AllGroupsLabel, "All Groups" },
                { LocalizationKey.NoGroupsYet, "No groups yet. Create one with the NEW GROUP button." },
                { LocalizationKey.NoGroupsMatchFilter, "No groups match this filter." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Show all districts" },
                { LocalizationKey.ShowGroupOverlayLabel, "Show group overlay" },
                { LocalizationKey.ShowServiceBuildingsLabel, "Show service buildings" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Permanently delete the group." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Assigned buildings will lose their **operating districts**." },
                { LocalizationKey.TypePickerTooltipLine1, "Change the **type** of the group." },
                { LocalizationKey.TypePickerTooltipLine2, "**Generic** groups can be assigned to any service building." },
                { LocalizationKey.TypePickerTooltipLine3, "All other types are only available to matching service buildings." },
                { LocalizationKey.DeleteGroupConfirmMessage, "\"{NAME}\" is assigned to {COUNT} service building(s).\nAssigned service building(s) will serve the whole city again." },
                { LocalizationKey.DeleteGroupDialogTitle, "Delete District Group?" },
                { LocalizationKey.DeleteGroupConfirm, "Delete group" },
                { LocalizationKey.DeleteGroupCancel, "Keep group" },
                { LocalizationKey.RemoveMemberTooltip, "Remove the district from the group." },
                { LocalizationKey.SelectDistrictsButton, "Select Districts" },
                { LocalizationKey.GroupColorTooltip, "Group Color" },
                { LocalizationKey.NameInputTooltip, "Select to edit the name." },
                { LocalizationKey.MetadataDistrictsTooltip, "Districts" },
                { LocalizationKey.MetadataBuildingsTooltip, "Assigned buildings" },
                { LocalizationKey.MetadataPopulationTooltip, "Population" },

                { LocalizationKey.ToggleTooltipTitle, "**DISTRICT GROUPS**" },
                { LocalizationKey.ToggleTooltipBody, "Create groups of districts to assign to service buildings for self-managing of **operating districts**." },

                { LocalizationKey.SectionTooltipLine1, "Service buildings can be assigned to a **district group**." },
                { LocalizationKey.SectionTooltipLine2, "When assigned, the group will manage the **operating districts** for the building." },
                { LocalizationKey.SectionTooltipLine3, "When unassigned, **operating districts** are controlled manually." },
                { LocalizationKey.SectionTooltipLine4, "NOTE: The Info Panel can take a few seconds to visually update after changing the assignment." },
                { LocalizationKey.SectionLabel, "DISTRICT GROUP" },
                { LocalizationKey.OperatingDistrictsLabel, "Operating Districts" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "This building is currently assigned to a district group." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**City district** assignments are controlled by the **assigned district group**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "If the **district group** does not have **city districts**, this building will provide services everywhere within their **operational radius**." },
                { LocalizationKey.UnassignOption, "Unassign" },
                { LocalizationKey.UnassignTooltipDisabled, "No group is assigned." },
                { LocalizationKey.UnassignTooltipEnabled, "Removes the current group assignment." },
                { LocalizationKey.UnassignedLabel, "Unassigned" },
                { LocalizationKey.GroupSearchTitle, "Select District Group" },
                { LocalizationKey.SearchGroupsPlaceholder, "Search..." },
                { LocalizationKey.NoGroupsMatchSearch, "No groups match your search." },
                { LocalizationKey.NoGroupsInSection, "No groups found." },

                { LocalizationKey.TypeGeneric, "Generic" },
                { LocalizationKey.TypePolice, "Police" },
                { LocalizationKey.TypeFire, "Fire" },
                { LocalizationKey.TypeHealthcare, "Healthcare" },
                { LocalizationKey.TypeDeathcare, "Deathcare" },
                { LocalizationKey.TypeGarbage, "Garbage" },
                { LocalizationKey.TypeEducationElementary, "Elementary School" },
                { LocalizationKey.TypeEducationHighSchool, "High School" },
                { LocalizationKey.TypeEducationCollege, "College" },
                { LocalizationKey.TypeEducationUniversity, "University" },
                { LocalizationKey.TypePost, "Post" },
                { LocalizationKey.TypeParks, "Parks" },
                { LocalizationKey.TypeWelfare, "Welfare" },
            };
        }

        public void Unload() { }
    }
}

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
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "Overlay UI" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Troubleshooting" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Border width" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Width of the colored district-group boundary lines drawn on the map." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "Border opacity" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacity of the colored district-group boundary lines drawn on the map.\n\n0% is fully transparent, 100% is fully opaque." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "Height offset" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "An offset from the height of the terrain at which the district-group overlay is drawn." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Scene desaturation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "How much the rest of the scene is desaturated while the group overlay is visible.\n\n0% leaves the scene untouched, 100% is grayscale." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Fill saturation" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "How saturated the colored district-group fill areas are.\n\n100% is the group's full color; lower values fade towards gray." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Enable debug logging" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Writes verbose Debug-level entries to the mod's log file.\n\nThis may affect performance." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Log debug mod data" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Writes all mod state (groups, service buildings, etc.) to the mod's log file.\n\nInclude the the log file when filing a bug report." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "File a bug" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Logs debug data, then opens the mod's GitHub issue tracker in your browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Reset all settings" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Resets all mod settings back to their default values." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "All mod settings will revert to their default values.\r\nDo you want to proceed?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "The installed version of the mod.\n\nInclude this when filing a bug report." },

                { "DistrictGroups.UI[PanelTitle]", "District Groups" },
                { "DistrictGroups.UI[NewGroupButton]", "New Group" },
                { "DistrictGroups.UI[NewGroupButtonTooltip]", "Adds a new group with no member districts." },
                { "DistrictGroups.UI[NewGroupDefaultName]", "New Group {NUMBER}" },
                { "DistrictGroups.UI[FilterTooltipLine1]", "Filter the list of groups by their **type**." },
                { "DistrictGroups.UI[AllGroupsLabel]", "All Groups" },
                { "DistrictGroups.UI[NoGroupsYet]", "No groups yet. Create one with the NEW GROUP button." },
                { "DistrictGroups.UI[NoGroupsMatchFilter]", "No groups match this filter." },
                { "DistrictGroups.UI[DisplayDistrictAreasLabel]", "Show all districts" },
                { "DistrictGroups.UI[ShowGroupOverlayLabel]", "Show group overlay" },

                { "DistrictGroups.UI[DeleteGroupTooltipLine1]", "Permanently delete the group." },
                { "DistrictGroups.UI[DeleteGroupTooltipLine2]", "Assigned buildings will lose their **operating districts**." },
                { "DistrictGroups.UI[TypePickerTooltipLine1]", "Change the **type** of the group." },
                { "DistrictGroups.UI[TypePickerTooltipLine2]", "**Generic** groups can be assigned to any service building." },
                { "DistrictGroups.UI[TypePickerTooltipLine3]", "All other types are only available to matching service buildings." },
                { "DistrictGroups.UI[DeleteGroupConfirmMessage]", "\"{NAME}\" is assigned to {COUNT} service building(s).\nAssigned service building(s) will serve the whole city again." },
                { "DistrictGroups.UI[DeleteGroupDialogTitle]", "Delete District Group?" },
                { "DistrictGroups.UI[DeleteGroupConfirm]", "Delete group" },
                { "DistrictGroups.UI[DeleteGroupCancel]", "Keep group" },
                { "DistrictGroups.UI[RemoveMemberTooltip]", "Remove the district from the group." },
                { "DistrictGroups.UI[SelectDistrictsButton]", "Select Districts" },
                { "DistrictGroups.UI[GroupColorTooltip]", "Group Color" },

                { "DistrictGroups.UI[ToggleTooltipTitle]", "**DISTRICT GROUPS**" },
                { "DistrictGroups.UI[ToggleTooltipBody]", "Create groups of districts to assign to service buildings for self-managing of **operating districts**." },

                { "DistrictGroups.UI[SectionTooltipLine1]", "Service buildings can be assigned to a **district group**." },
                { "DistrictGroups.UI[SectionTooltipLine2]", "When assigned, the group will manage the **operating districts** for the building." },
                { "DistrictGroups.UI[SectionTooltipLine3]", "When unassigned, **operating districts** are managed manually." },
                { "DistrictGroups.UI[SectionTooltipLine4]", "NOTE: The Info Panel can take a few seconds to visually update after changing the assignment." },
                { "DistrictGroups.UI[SectionLabel]", "DISTRICT GROUP" },
                { LocalizationKey.OperatingDistrictsLabel, "Operating Districts" },
                { "DistrictGroups.UI[UnassignOption]", "Unassign" },
                { "DistrictGroups.UI[UnassignTooltipDisabled]", "No group is assigned." },
                { "DistrictGroups.UI[UnassignTooltipEnabled]", "Removes the current group assignment." },
                { "DistrictGroups.UI[UnassignedLabel]", "Unassigned" },
                { "DistrictGroups.UI[GroupSearchTitle]", "Select District Group" },
                { "DistrictGroups.UI[SearchGroupsPlaceholder]", "Search..." },
                { "DistrictGroups.UI[NoGroupsMatchSearch]", "No groups match your search." },

                { "DistrictGroups.UI[TypeGeneric]", "Generic" },
                { "DistrictGroups.UI[TypePolice]", "Police" },
                { "DistrictGroups.UI[TypeFire]", "Fire" },
                { "DistrictGroups.UI[TypeHealthcare]", "Healthcare" },
                { "DistrictGroups.UI[TypeDeathcare]", "Deathcare" },
                { "DistrictGroups.UI[TypeGarbage]", "Garbage" },
                { "DistrictGroups.UI[TypeEducationElementary]", "Elementary School" },
                { "DistrictGroups.UI[TypeEducationHighSchool]", "High School" },
                { "DistrictGroups.UI[TypeEducationCollege]", "College" },
                { "DistrictGroups.UI[TypeEducationUniversity]", "University" },
                { "DistrictGroups.UI[TypePost]", "Post" },
                { "DistrictGroups.UI[TypeParks]", "Parks" },
                { "DistrictGroups.UI[TypeWelfare]", "Welfare" },
            };
        }

        public void Unload() { }
    }
}

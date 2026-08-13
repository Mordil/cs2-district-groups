using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    // English text for the React UI panel/tooltips (src/UI/src). Key strings
    // here must match src/UI/src/locale.ts's id() output exactly - the
    // "DistrictGroups.UI[Key]" scheme mirrors the game's own locale id style
    // (e.g. "Options.SECTION[...]") so other locales can register their own
    // translations for the same keys via additional IDictionarySource classes.
    public class UILocaleEN : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { "DistrictGroups.UI[PanelTitle]", "District Groups" },
                { "DistrictGroups.UI[NewGroupButton]", "New Group" },
                { "DistrictGroups.UI[NewGroupButtonTooltip]", "Adds a new group with no member districts." },
                { "DistrictGroups.UI[NewGroupDefaultName]", "New Group {NUMBER}" },
                { "DistrictGroups.UI[FilterTooltipLine1]", "Filter the list of groups by their **type**." },
                { "DistrictGroups.UI[FilterTooltipLine2]", "If **All Groups** is selected, then all groups will be listed." },
                { "DistrictGroups.UI[AllGroupsLabel]", "All Groups" },
                { "DistrictGroups.UI[NoGroupsYet]", "No groups yet. Create one above." },
                { "DistrictGroups.UI[NoGroupsMatchFilter]", "No groups match this filter." },
                { "DistrictGroups.UI[DisplayDistrictAreasLabel]", "Display District areas" },

                { "DistrictGroups.UI[DeleteGroupTooltipLine1]", "Permanently delete the group." },
                { "DistrictGroups.UI[DeleteGroupTooltipLine2]", "Assigned buildings will lose their **operating districts**." },
                { "DistrictGroups.UI[TypePickerTooltipLine1]", "Change the **type** of the group." },
                { "DistrictGroups.UI[TypePickerTooltipLine2]", "**Generic** groups can be assigned to any service building." },
                { "DistrictGroups.UI[TypePickerTooltipLine3]", "All other types are only available to matching service buildings." },
                { "DistrictGroups.UI[DeleteGroupConfirmMessage]", "\"{NAME}\" is assigned to {COUNT} service building(s).\nAssigned service building(s) will serve the whole city again." },
                { "DistrictGroups.UI[DeleteGroupDialogTitle]", "Delete District Group?" },
                { "DistrictGroups.UI[DeleteGroupConfirm]", "Keep group" },
                { "DistrictGroups.UI[DeleteGroupCancel]", "Delete group" },
                { "DistrictGroups.UI[RemoveMemberTooltip]", "Remove the district from the group." },
                { "DistrictGroups.UI[SelectDistrictsButton]", "Select Districts" },

                { "DistrictGroups.UI[ToggleTooltipTitle]", "**DISTRICT GROUPS**" },
                { "DistrictGroups.UI[ToggleTooltipBody]", "Create groups of districts to assign to service buildings for self-managing of **operating districts**." },
                { "DistrictGroups.UI[ToggleTooltipCount]", "Existing groups: {COUNT}" },

                { "DistrictGroups.UI[SectionTooltipLine1]", "Service buildings can be assigned to a **district group**." },
                { "DistrictGroups.UI[SectionTooltipLine2]", "When assigned, the group will manage the **operating districts** for the building." },
                { "DistrictGroups.UI[SectionTooltipLine3]", "When unassigned, **operating districts** are managed manually." },
                { "DistrictGroups.UI[SectionLabel]", "DISTRICT GROUP" },
                { "DistrictGroups.UI[UnassignOption]", "None (Unassign)" },
                { "DistrictGroups.UI[UnassignedLabel]", "Unassigned" },

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

        public void Unload()
        {
        }
    }
}

using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Policies;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using static DistrictGroups.EntityJson;

namespace DistrictGroups
{
    public partial class DistrictGroupsUISystem : UISystemBase
    {
        /* This needs to be the same as in mod.json */
        public const string kBindingGroup = "district-groups";

        // Sent as a building's efficiency when the game has none to report for it, so the UI can show a placeholder instead of a misleading zero.
        public const int kUnknownEfficiency = -1;

        private DistrictGroupSystem m_GroupSystem;
        private DistrictStatsSystem m_StatsSystem;
        private DistrictGroupOverlaySystem m_OverlaySystem;
        private DistrictGroupServiceBuildingSystem m_ServiceBuildingSystem;
        private DistrictGroupSelectionSystem m_SelectionSystem;
        private DistrictGroupPolicySystem m_PolicySystem;
        private NameSystem m_NameSystem;
        private PrefabSystem m_PrefabSystem;
        private PrefabUISystem m_PrefabUISystem;
        private SelectedInfoUISystem m_SelectedInfoUISystem;
        private CameraUpdateSystem m_CameraUpdateSystem;
        private ToolSystem m_ToolSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private AreaToolSystem m_AreaToolSystem;
        private SelectionToolSystem m_SelectionToolSystem;
        private GamePanelUISystem m_GamePanelUISystem;
        private EntityQuery m_GroupQuery;

        // Remembers whatever the vanilla info panel was showing (if anything)
        // at the moment our panel opened, so closing our panel restores it —
        // the two panels share the same screen corner and shouldn't compete.
        private Entity m_SavedSelection = Entity.Null;

        private RawValueBinding m_GroupsBinding;
        private RawValueBinding m_ServiceBuildingsBinding;
        private RawValueBinding m_SelectingGroupBinding;
        private RawValueBinding m_GroupPoliciesBinding;
        private int m_LastSeenGroupVersion = -1;
        private int m_LastSeenStatsVersion = -1;
        private int m_LastSeenTypeFilter = -1;
        private int m_LastSeenTargetVersion;
        private int m_LastSeenPolicyVersion = -1;
        private int m_LastSeenGroupCompositionVersion = -1;
        private Entity m_LastSeenFocusedGroup = Entity.Null;
        private Entity m_LastSeenSelectingGroup = Entity.Null;
        private int m_LastSeenGroupsRefreshVersion = RefreshClock.kNeverRefreshed;
        private int m_LastSeenServiceBuildingsRefreshVersion = RefreshClock.kNeverRefreshed;

        // Scratch space for the focused group's member districts, reused by every policy payload.
        private readonly List<PolicyDistrict> m_PolicyDistricts = new List<PolicyDistrict>();

        // One member district as the policy payload reads it, holding what every policy would otherwise resolve again.
        private readonly struct PolicyDistrict
        {
            public readonly Entity m_District;
            public readonly string m_Name;
            public readonly DynamicBuffer<Policy> m_Policies;

            public PolicyDistrict(Entity district, string name, DynamicBuffer<Policy> policies)
            {
                m_District = district;
                m_Name = name;
                m_Policies = policies;
            }
        }

        // Lets the UI know if we're in a debug build
        public static bool IsDebugBuild =>
#if DEBUG
            true;
#else
            false;
#endif

        protected override void OnCreate()
        {
            base.OnCreate();

            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_StatsSystem = World.GetOrCreateSystemManaged<DistrictStatsSystem>();
            m_OverlaySystem = World.GetOrCreateSystemManaged<DistrictGroupOverlaySystem>();
            m_ServiceBuildingSystem = World.GetOrCreateSystemManaged<DistrictGroupServiceBuildingSystem>();
            m_SelectionSystem = World.GetOrCreateSystemManaged<DistrictGroupSelectionSystem>();
            m_PolicySystem = World.GetOrCreateSystemManaged<DistrictGroupPolicySystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_PrefabUISystem = World.GetOrCreateSystemManaged<PrefabUISystem>();
            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            m_CameraUpdateSystem = World.GetOrCreateSystemManaged<CameraUpdateSystem>();
            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_AreaToolSystem = World.GetOrCreateSystemManaged<AreaToolSystem>();
            m_SelectionToolSystem = World.GetOrCreateSystemManaged<SelectionToolSystem>();
            m_GamePanelUISystem = World.GetOrCreateSystemManaged<GamePanelUISystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());

            SetupRootBindings();
            SetupOverlayBindings();
            SetupGroupManagementPanelBindings();
            SetupPolicyBindings();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            /*
            Update UI bindings with the latest data if it's been mutated or if the refresh interval says it's time to update

            This is to avoid writing data to the UI that hasn't changed, every single frame...
            */

            int groupVersion = m_GroupSystem.Version;
            int statsVersion = m_StatsSystem.StatsVersion;
            int typeFilter = m_OverlaySystem.TypeFilter;
            int targetVersion = m_ServiceBuildingSystem.TargetVersion;
            Entity selectingGroup = m_SelectionSystem.SelectingGroup;

            bool mutated = groupVersion != m_LastSeenGroupVersion;
            // The stats sweep lands a frame or more after it was asked for, so its totals arrive on their own.
            bool statsArrived = statsVersion != m_LastSeenStatsVersion;
            bool filterChanged = typeFilter != m_LastSeenTypeFilter;
            bool targetsChanged = targetVersion != m_LastSeenTargetVersion;

            m_LastSeenGroupVersion = groupVersion;
            m_LastSeenStatsVersion = statsVersion;
            m_LastSeenTypeFilter = typeFilter;
            m_LastSeenTargetVersion = targetVersion;

            // To avoid heavy UI updates on a single frame, they're staggered into different phases
            bool groupsRefreshDue = RefreshClock.IsDue(RefreshPhase.Groups, ref m_LastSeenGroupsRefreshVersion);
            bool serviceBuildingsRefreshDue = RefreshClock.IsDue(
                RefreshPhase.ServiceBuildings,
                ref m_LastSeenServiceBuildingsRefreshVersion);

            // Asks for a fresh sweep; it lands a frame or more later and reports itself through statsArrived
            if (groupsRefreshDue)
            {
                m_StatsSystem.InvalidateDistrictStats();
            }

            if (mutated || statsArrived || groupsRefreshDue)
            {
                m_GroupsBinding.Update();
            }

            if (mutated || filterChanged || targetsChanged || serviceBuildingsRefreshDue)
            {
                m_ServiceBuildingsBinding.Update();
            }

            if (selectingGroup != m_LastSeenSelectingGroup)
            {
                m_LastSeenSelectingGroup = selectingGroup;
                m_SelectingGroupBinding.Update();
            }

            int policyVersion = m_PolicySystem.Version;
            int compositionVersion = m_GroupSystem.GroupCompositionVersion;
            Entity focusedGroup = m_GroupSystem.FocusedGroup;

            bool policiesChanged = policyVersion != m_LastSeenPolicyVersion;
            bool membersChanged = compositionVersion != m_LastSeenGroupCompositionVersion;
            bool focusChanged = focusedGroup != m_LastSeenFocusedGroup;

            m_LastSeenPolicyVersion = policyVersion;
            m_LastSeenGroupCompositionVersion = compositionVersion;
            m_LastSeenFocusedGroup = focusedGroup;

            if (policiesChanged || membersChanged || focusChanged)
            {
                m_GroupPoliciesBinding.Update();
            }
        }

        private bool IsCameraBlockingPanel()
        {
            IGameCameraController active = m_CameraUpdateSystem.activeCameraController;

            if (ReferenceEquals(active, m_CameraUpdateSystem.gamePlayController))
            {
                return false;
            }

            return !ReferenceEquals(active, m_CameraUpdateSystem.orbitCameraController)
                || m_CameraUpdateSystem.orbitCameraController.mode != Game.OrbitCameraController.Mode.Follow;
        }

        // Global bindings not scoped to any one panel
        private void SetupRootBindings()
        {
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "isDebugBuild",
                () => IsDebugBuild));

            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "selectedBuildingHasGroupAssignment",
                () => EntityManager.Exists(m_SelectedInfoUISystem.selectedEntity)
                    && m_GroupSystem.IsBuildingAssigned(m_SelectedInfoUISystem.selectedEntity)));

            AddBinding(new TriggerBinding<string, string>(kBindingGroup, "log", LogFromUI));
        }

        private void SetupOverlayBindings()
        {
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "areasVisible",
                () => m_OverlaySystem.AreasVisible));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "showOverlay",
                () => m_OverlaySystem.ShowOverlay));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "areaToolActive",
                () => m_OverlaySystem.IsAreaToolActive));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "overlayVisible",
                () => m_OverlaySystem.Visible));
            // Everything that means our panel doesn't belong on screen right now
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "shouldDismissPanel",
                () => m_GamePanelUISystem.activePanel is InfoviewMenu
                    || m_SelectedInfoUISystem.selectedEntity != Entity.Null
                    || IsCameraBlockingPanel()
                    || (m_ToolSystem.activeTool != null
                        && m_ToolSystem.activeTool != m_DefaultToolSystem
                        && m_ToolSystem.activeTool != m_AreaToolSystem
                        && m_ToolSystem.activeTool != m_SelectionToolSystem)));

            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setOverlay", OnPanelOpenChanged));
            AddBinding(new TriggerBinding<int>(kBindingGroup, "setOverlayFilter",
                type => m_OverlaySystem.SetTypeFilter(type)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setAreasVisible",
                visible => m_OverlaySystem.SetAreasVisible(visible)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setShowOverlay",
                show => m_OverlaySystem.SetShowOverlay(show)));

            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "showServiceBuildings",
                () => m_ServiceBuildingSystem.ShowServiceBuildings));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setShowServiceBuildings",
                show => m_ServiceBuildingSystem.SetShowServiceBuildings(show)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setHideAssignedBuildings",
                hide => m_ServiceBuildingSystem.SetHideAssignedBuildings(hide)));
        }

        private void SetupPolicyBindings()
        {
            m_GroupPoliciesBinding = new RawValueBinding(kBindingGroup, "groupPolicies", WriteGroupPolicies);
            AddBinding(m_GroupPoliciesBinding);

            AddBinding(new TriggerBinding<Entity, Entity, bool>(kBindingGroup, "setGroupPolicyActive",
                (group, policy, active) => m_PolicySystem.SetGroupPolicyActive(group, policy, active)));
            AddBinding(new TriggerBinding<Entity, Entity, float>(kBindingGroup, "setGroupPolicyValue",
                (group, policy, value) => m_PolicySystem.SetGroupPolicyValue(group, policy, value)));
            AddBinding(new TriggerBinding<Entity, Entity, bool>(kBindingGroup, "setDistrictPolicyActive",
                (district, policy, active) => m_PolicySystem.SetDistrictPolicyActive(district, policy, active)));
            AddBinding(new TriggerBinding<Entity, Entity, float>(kBindingGroup, "setDistrictPolicyValue",
                (district, policy, value) => m_PolicySystem.SetDistrictPolicyValue(district, policy, value)));
        }

        private void SetupGroupManagementPanelBindings()
        {
            m_GroupsBinding = new RawValueBinding(kBindingGroup, "groups", WriteGroups);
            m_ServiceBuildingsBinding = new RawValueBinding(kBindingGroup, "serviceBuildings", WriteServiceBuildings);
            m_SelectingGroupBinding = new RawValueBinding(kBindingGroup, "selectingGroup",
                writer => WriteEntity(writer, m_SelectionSystem.SelectingGroup));
            AddBinding(m_GroupsBinding);
            AddBinding(m_ServiceBuildingsBinding);
            AddBinding(m_SelectingGroupBinding);

            AddBinding(new TriggerBinding(kBindingGroup, "createGroup",
                () => m_GroupSystem.CreateGroup((GroupServiceType)m_OverlaySystem.TypeFilter)));
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "deleteGroup",
                group => m_GroupSystem.DeleteGroup(group)));
            AddBinding(new TriggerBinding<Entity, string>(kBindingGroup, "renameGroup",
                (group, name) => m_GroupSystem.RenameGroup(group, name)));
            AddBinding(new TriggerBinding<Entity, int>(kBindingGroup, "setGroupType",
                (group, type) => m_GroupSystem.SetGroupType(group, (GroupServiceType)type)));
            AddBinding(new TriggerBinding<Entity, Color>(kBindingGroup, "setGroupColor",
                (group, color) => m_GroupSystem.SetGroupColor(group, color)));
            AddBinding(new TriggerBinding<Entity, Entity>(kBindingGroup, "removeMember",
                (group, district) =>
                {
                    m_GroupSystem.RemoveMember(group, district);
                    m_SelectionSystem.NotifyMemberRemoved(group);
                }));
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "toggleDistrictSelection",
                group => m_SelectionSystem.ToggleSelection(group)));

            // The group info panel narrows the overlay and the service-building markers to the one
            // group it shows; clearing widens both back out to the panel's filtered type.
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "setFocusedGroup",
                group => m_GroupSystem.SetFocusedGroup(group)));
            AddBinding(new TriggerBinding(kBindingGroup, "clearFocusedGroup",
                () => m_GroupSystem.SetFocusedGroup(Entity.Null)));

            // The info-panel section's own "assignGroup"/"unassignGroup" always mean
            // the selected building; these name the building instead, since the
            // panel assigns buildings it never selects.
            AddBinding(new TriggerBinding<Entity, Entity>(kBindingGroup, "assignBuildingGroup",
                (building, group) => m_GroupSystem.AssignBuilding(building, group)));
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "unassignBuildingGroup",
                building => m_GroupSystem.UnassignBuilding(building)));
        }
    }
}

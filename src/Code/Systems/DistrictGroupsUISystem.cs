using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
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

        private DistrictGroupSystem m_GroupSystem;
        private DistrictGroupOverlaySystem m_OverlaySystem;
        private DistrictGroupServiceBuildingSystem m_ServiceBuildingSystem;
        private DistrictGroupSelectionSystem m_SelectionSystem;
        private NameSystem m_NameSystem;
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

        private readonly List<Entity> m_ServiceBuildingSample = new List<Entity>();
        private GroupServiceType m_SampledServiceType = GroupServiceType.Generic;
        private float m_LastServiceBuildingSampleTime = float.NegativeInfinity;

        private RawValueBinding m_GroupsBinding;
        private RawValueBinding m_ServiceBuildingsBinding;
        private RawValueBinding m_SelectingGroupBinding;
        private int m_LastSeenGroupVersion = -1;
        private int m_LastSeenTypeFilter = -1;
        private Entity m_LastSeenSelectingGroup = Entity.Null;
        private float m_LastPanelRefreshTime = float.NegativeInfinity;

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
            m_OverlaySystem = World.GetOrCreateSystemManaged<DistrictGroupOverlaySystem>();
            m_ServiceBuildingSystem = World.GetOrCreateSystemManaged<DistrictGroupServiceBuildingSystem>();
            m_SelectionSystem = World.GetOrCreateSystemManaged<DistrictGroupSelectionSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
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
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            /*
            Update UI bindings with the latest data if it's been mutated or if the refresh interval says it's time to update

            This is to avoid writing data to the UI that hasn't changed, every single frame...
            */

            int groupVersion = m_GroupSystem.Version;
            int typeFilter = m_OverlaySystem.TypeFilter;
            Entity selectingGroup = m_SelectionSystem.SelectingGroup;
            float refreshRateSeconds = Mod.Settings?.RefreshRateSeconds ?? Setting.kDefaultRefreshRateSeconds;

            bool mutated = groupVersion != m_LastSeenGroupVersion;
            bool filterChanged = typeFilter != m_LastSeenTypeFilter;
            bool refreshDue = UnityEngine.Time.realtimeSinceStartup - m_LastPanelRefreshTime >= refreshRateSeconds;

            m_LastSeenGroupVersion = groupVersion;
            m_LastSeenTypeFilter = typeFilter;
            if (refreshDue)
            {
                m_LastPanelRefreshTime = UnityEngine.Time.realtimeSinceStartup;
            }

            if (mutated || refreshDue)
            {
                m_GroupsBinding.Update();
            }

            if (mutated || filterChanged || refreshDue)
            {
                m_ServiceBuildingsBinding.Update();
            }

            if (selectingGroup != m_LastSeenSelectingGroup)
            {
                m_LastSeenSelectingGroup = selectingGroup;
                m_SelectingGroupBinding.Update();
            }
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
                    || !ReferenceEquals(m_CameraUpdateSystem.activeCameraController, m_CameraUpdateSystem.gamePlayController)
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
        }

        private void SetupGroupManagementPanelBindings()
        {
            // AddBinding, not AddUpdateBinding: OnUpdate decides when each of these
            // has something new to write. They still push their first payload on
            // their own, when the panel subscribes.
            m_GroupsBinding = new RawValueBinding(kBindingGroup, "groups", WriteGroups);
            m_ServiceBuildingsBinding = new RawValueBinding(kBindingGroup, "serviceBuildings", WriteServiceBuildings);
            m_SelectingGroupBinding = new RawValueBinding(kBindingGroup, "selectingGroup",
                writer => WriteEntity(writer, m_SelectionSystem.SelectingGroup));
            AddBinding(m_GroupsBinding);
            AddBinding(m_ServiceBuildingsBinding);
            AddBinding(m_SelectingGroupBinding);

            AddBinding(new TriggerBinding<string, int>(kBindingGroup, "createGroup",
                (name, type) => m_GroupSystem.CreateGroup(name, (GroupServiceType)type)));
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

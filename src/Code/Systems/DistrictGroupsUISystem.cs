using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
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
            AddUpdateBinding(new RawValueBinding(kBindingGroup, "groups", WriteGroups));
            AddUpdateBinding(new RawValueBinding(kBindingGroup, "selectingGroup",
                writer => WriteEntity(writer, m_SelectionSystem.SelectingGroup)));

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
        }
    }
}

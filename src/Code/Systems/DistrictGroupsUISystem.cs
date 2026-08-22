using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
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
        private DistrictGroupSelectionSystem m_SelectionSystem;
        private NameSystem m_NameSystem;
        private SelectedInfoUISystem m_SelectedInfoUISystem;
        private EntityQuery m_GroupQuery;
        private ValueBinding<int> m_IconStyleBinding;

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
            m_SelectionSystem = World.GetOrCreateSystemManaged<DistrictGroupSelectionSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
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

            AddBinding(new TriggerBinding<string, string>(kBindingGroup, "log", LogFromUI));

            m_IconStyleBinding = new ValueBinding<int>(kBindingGroup, "iconStyle",
                (int)(Mod.Settings?.IconStyle ?? Setting.kDefaultIconStyle));
            AddBinding(m_IconStyleBinding);
        }

        public void SetIconStyle(ModIconStyle style) => m_IconStyleBinding.Update((int)style);

        private void SetupOverlayBindings()
        {
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "areasVisible",
                () => m_OverlaySystem.AreasVisible));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "showOverlay",
                () => m_OverlaySystem.ShowOverlay));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "areaToolActive",
                () => m_OverlaySystem.IsAreaToolActive));

            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setOverlay", OnPanelOpenChanged));
            AddBinding(new TriggerBinding<int>(kBindingGroup, "setOverlayFilter",
                type => m_OverlaySystem.SetTypeFilter(type)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setAreasVisible",
                visible => m_OverlaySystem.SetAreasVisible(visible)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setShowOverlay",
                show => m_OverlaySystem.SetShowOverlay(show)));
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
                (group, district) => m_GroupSystem.RemoveMember(group, district)));
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "toggleDistrictSelection",
                group => m_SelectionSystem.ToggleSelection(group)));
        }
    }
}

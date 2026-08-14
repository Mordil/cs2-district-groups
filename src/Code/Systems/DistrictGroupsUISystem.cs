using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;

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
        private EntityQuery m_DistrictQuery;

        // Remembers whatever the vanilla info panel was showing (if anything)
        // at the moment our panel opened, so closing our panel restores it —
        // the two panels share the same screen corner and shouldn't compete.
        private Entity m_SavedSelection = Entity.Null;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_OverlaySystem = World.GetOrCreateSystemManaged<DistrictGroupOverlaySystem>();
            m_SelectionSystem = World.GetOrCreateSystemManaged<DistrictGroupSelectionSystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_SelectedInfoUISystem = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());

            AddUpdateBinding(new RawValueBinding(kBindingGroup, "groups", WriteGroups));
            AddUpdateBinding(new RawValueBinding(kBindingGroup, "districts", WriteDistricts));
            AddUpdateBinding(new GetterValueBinding<bool>(kBindingGroup, "areasVisible",
                () => m_OverlaySystem.AreasVisible));
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
            AddBinding(new TriggerBinding<Entity, Entity>(kBindingGroup, "addMember",
                (group, district) => m_GroupSystem.AddMember(group, district)));
            AddBinding(new TriggerBinding<Entity, Entity>(kBindingGroup, "removeMember",
                (group, district) => m_GroupSystem.RemoveMember(group, district)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setOverlay", OnPanelOpenChanged));
            AddBinding(new TriggerBinding<int>(kBindingGroup, "setOverlayFilter",
                type => m_OverlaySystem.SetTypeFilter(type)));
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setAreasVisible",
                visible => m_OverlaySystem.SetAreasVisible(visible)));
            AddBinding(new TriggerBinding<Entity>(kBindingGroup, "toggleDistrictSelection",
                group => m_SelectionSystem.ToggleSelection(group)));
            AddBinding(new TriggerBinding<string, string>(kBindingGroup, "log", LogFromUI));
        }

        // Lets the React UI route its own logs into the mod's log file
        // alongside C# logs, tagged so their origin is obvious.
        private static void LogFromUI(string level, string message)
        {
            string tagged = $"[UI] {message}";
            switch (level)
            {
                case "debug": Mod.log.Debug(tagged); break;
                case "warn": Mod.log.Warn(tagged); break;
                case "error": Mod.log.Error(tagged); break;
                case "critical": Mod.log.Critical(tagged); break;
                default: Mod.log.Info(tagged); break;
            }
        }

        // "setOverlay" fires exactly at our panel's open/close, so it doubles
        // as the signal for closing/restoring the vanilla selected-info panel.
        private void OnPanelOpenChanged(bool open)
        {
            m_OverlaySystem.SetVisible(open);

            if (open)
            {
                m_SavedSelection = m_SelectedInfoUISystem.selectedEntity;
                if (m_SavedSelection != Entity.Null)
                {
                    m_SelectedInfoUISystem.SetSelection(Entity.Null);
                }
            }
            else
            {
                if (m_SavedSelection != Entity.Null && EntityManager.Exists(m_SavedSelection))
                {
                    m_SelectedInfoUISystem.SetSelection(m_SavedSelection);
                }
                m_SavedSelection = Entity.Null;
            }
        }

        private void WriteGroups(IJsonWriter writer)
        {
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            writer.ArrayBegin(groups.Length);
            foreach (Entity group in groups)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(group);
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(group, isReadOnly: true);
                writer.TypeBegin("Group");
                writer.PropertyName("entity");
                WriteEntity(writer, group);
                writer.PropertyName("name");
                writer.Write(data.m_Name.ToString());
                writer.PropertyName("type");
                writer.Write((int)data.m_Type);
                writer.PropertyName("assignedBuildingCount");
                using (NativeArray<Entity> assignedBuildings = m_GroupSystem.GetAssignedBuildings(group, Allocator.Temp))
                {
                    writer.Write(assignedBuildings.Length);
                }
                writer.PropertyName("members");
                writer.ArrayBegin(members.Length);
                foreach (DistrictGroupMember member in members)
                {
                    WriteNamedEntity(writer, member.m_District);
                }
                writer.ArrayEnd();
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private void WriteDistricts(IJsonWriter writer)
        {
            using NativeArray<Entity> districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            writer.ArrayBegin(districts.Length);
            foreach (Entity district in districts)
            {
                WriteNamedEntity(writer, district);
            }
            writer.ArrayEnd();
        }

        private void WriteNamedEntity(IJsonWriter writer, Entity entity)
        {
            writer.TypeBegin("NamedEntity");
            writer.PropertyName("entity");
            WriteEntity(writer, entity);
            writer.PropertyName("name");
            writer.Write(EntityManager.Exists(entity) ? m_NameSystem.GetRenderedLabelName(entity) : "<missing>");
            writer.TypeEnd();
        }

        // Matches the JS-side Entity shape ({index, version}) explicitly, so the
        // wire format never depends on which writer extensions exist.
        private static void WriteEntity(IJsonWriter writer, Entity entity)
        {
            writer.TypeBegin("Unity.Entities.Entity");
            writer.PropertyName("index");
            writer.Write(entity.Index);
            writer.PropertyName("version");
            writer.Write(entity.Version);
            writer.TypeEnd();
        }
    }
}

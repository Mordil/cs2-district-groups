using Colossal.UI.Binding;
using Game.Areas;
using Game.Common;
using Game.Tools;
using Game.UI;
using Unity.Collections;
using Unity.Entities;

namespace DistrictGroups
{
    // Phase 5: C# side of the UI bindings. Binding group is the mod id from
    // UI/mod.json — keep the two in sync.
    public partial class DistrictGroupsUISystem : UISystemBase
    {
        public const string kBindingGroup = "district-groups";

        private DistrictGroupSystem m_GroupSystem;
        private DistrictGroupOverlaySystem m_OverlaySystem;
        private NameSystem m_NameSystem;
        private EntityQuery m_GroupQuery;
        private EntityQuery m_DistrictQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_OverlaySystem = World.GetOrCreateSystemManaged<DistrictGroupOverlaySystem>();
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());

            AddUpdateBinding(new RawValueBinding(kBindingGroup, "groups", WriteGroups));
            AddUpdateBinding(new RawValueBinding(kBindingGroup, "districts", WriteDistricts));

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
            AddBinding(new TriggerBinding<bool>(kBindingGroup, "setOverlay",
                visible => m_OverlaySystem.SetVisible(visible)));
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

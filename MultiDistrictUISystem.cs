using Colossal.UI.Binding;
using Game.UI;
using Unity.Entities;

namespace multi_district_tool
{
    // Phase 5: C# side of the UI bindings. Binding group is the mod id from
    // UI/mod.json — keep the two in sync.
    public partial class MultiDistrictUISystem : UISystemBase
    {
        public const string kBindingGroup = "multi-district-tool";

        private EntityQuery m_GroupQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());

            AddUpdateBinding(new GetterValueBinding<int>(kBindingGroup, "groupCount",
                () => m_GroupQuery.CalculateEntityCount()));
            AddBinding(new TriggerBinding(kBindingGroup, "test",
                () => Mod.log.Info("UI round-trip: test trigger received.")));
        }
    }
}

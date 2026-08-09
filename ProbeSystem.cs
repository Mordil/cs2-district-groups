using System;
using Game;
using Game.Areas;
using Game.Common;
using Game.Input;
using Game.Tools;
using Game.UI;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;

namespace multi_district_tool
{
    // Phase 1 probe (see CLAUDE_IMPL_PLAN.md): on-demand experiments triggered
    // from the options screen. Step 1.2 dumps district/selection state; step 1.3
    // writes a district into the selected building's ServiceDistrict buffer.
    public partial class ProbeSystem : GameSystemBase
    {
        public static bool DumpRequested;
        public static bool WriteRequested;

        private EntityQuery m_DistrictQuery;
        private NameSystem m_NameSystem;
        private SelectedInfoUISystem m_SelectedInfo;
        private ProxyAction m_DumpAction;
        private ProxyAction m_WriteAction;

        // Escape (to reach the options menu) clears the selection, so remember
        // the last selected entity; probes fall back to it.
        private Entity m_LastSelected;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DistrictQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.Exclude<Deleted>(),
                ComponentType.Exclude<Temp>());
            m_NameSystem = World.GetOrCreateSystemManaged<NameSystem>();
            m_SelectedInfo = World.GetOrCreateSystemManaged<SelectedInfoUISystem>();

            m_DumpAction = Mod.Settings?.GetAction(Setting.kDumpActionName);
            m_WriteAction = Mod.Settings?.GetAction(Setting.kWriteActionName);
            if (m_DumpAction != null) m_DumpAction.shouldBeEnabled = true;
            if (m_WriteAction != null) m_WriteAction.shouldBeEnabled = true;
        }

        protected override void OnUpdate()
        {
            Entity selected = m_SelectedInfo.selectedEntity;
            if (selected != Entity.Null && EntityManager.Exists(selected))
            {
                m_LastSelected = selected;
            }

            if (DumpRequested || (m_DumpAction?.WasPerformedThisFrame() ?? false))
            {
                DumpRequested = false;
                Run(nameof(DumpDistricts), DumpDistricts);
            }
            if (WriteRequested || (m_WriteAction?.WasPerformedThisFrame() ?? false))
            {
                WriteRequested = false;
                Run(nameof(WriteProbe), WriteProbe);
            }
        }

        private Entity GetTarget()
        {
            Entity selected = m_SelectedInfo.selectedEntity;
            if (selected != Entity.Null && EntityManager.Exists(selected))
            {
                return selected;
            }
            if (m_LastSelected != Entity.Null && EntityManager.Exists(m_LastSelected))
            {
                Mod.log.Info($"Nothing selected; using last selection {m_LastSelected}.");
                return m_LastSelected;
            }
            return Entity.Null;
        }

        private static void Run(string name, Action action)
        {
            try
            {
                Mod.log.Info($"--- {name} ---");
                action();
            }
            catch (Exception e)
            {
                Mod.log.Error($"{name} failed: {e}");
            }
        }

        // Step 1.2: read probe.
        private void DumpDistricts()
        {
            using NativeArray<Entity> districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            Mod.log.Info($"Districts in city: {districts.Length}");
            foreach (Entity district in districts)
            {
                Mod.log.Info($"  {district} \"{m_NameSystem.GetRenderedLabelName(district)}\"");
            }

            Entity selected = GetTarget();
            if (selected == Entity.Null)
            {
                Mod.log.Info("No entity selected (and no remembered selection).");
                return;
            }

            Mod.log.Info($"Selected: {selected} \"{m_NameSystem.GetRenderedLabelName(selected)}\"");

            if (EntityManager.HasComponent<CurrentDistrict>(selected))
            {
                CurrentDistrict currentDistrict = EntityManager.GetComponentData<CurrentDistrict>(selected);
                string name = currentDistrict.m_District != Entity.Null
                    ? m_NameSystem.GetRenderedLabelName(currentDistrict.m_District)
                    : "<none>";
                Mod.log.Info($"  CurrentDistrict: {currentDistrict.m_District} \"{name}\"");
            }
            else
            {
                Mod.log.Info("  No CurrentDistrict component.");
            }

            if (EntityManager.HasBuffer<ServiceDistrict>(selected))
            {
                DynamicBuffer<ServiceDistrict> buffer = EntityManager.GetBuffer<ServiceDistrict>(selected, isReadOnly: true);
                Mod.log.Info($"  ServiceDistrict buffer: {buffer.Length} entries");
                foreach (ServiceDistrict entry in buffer)
                {
                    Mod.log.Info($"    {entry.m_District} \"{m_NameSystem.GetRenderedLabelName(entry.m_District)}\"");
                }
            }
            else
            {
                Mod.log.Info("  No ServiceDistrict buffer (not a district-servable building).");
            }
        }

        // Step 1.3: write probe — the project's go/no-go gate.
        private void WriteProbe()
        {
            Entity selected = GetTarget();
            if (selected == Entity.Null)
            {
                Mod.log.Info("Select a service building first.");
                return;
            }
            if (!EntityManager.HasBuffer<ServiceDistrict>(selected))
            {
                Mod.log.Info($"Selected entity {selected} has no ServiceDistrict buffer; select a service building (police, school, ...).");
                return;
            }

            using NativeArray<Entity> districts = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            if (districts.Length == 0)
            {
                Mod.log.Info("No districts exist; paint one first.");
                return;
            }

            DynamicBuffer<ServiceDistrict> buffer = EntityManager.GetBuffer<ServiceDistrict>(selected);
            foreach (Entity district in districts)
            {
                bool alreadyServed = false;
                foreach (ServiceDistrict entry in buffer)
                {
                    if (entry.m_District == district)
                    {
                        alreadyServed = true;
                        break;
                    }
                }
                if (!alreadyServed)
                {
                    buffer.Add(new ServiceDistrict(district));
                    Mod.log.Info($"Added district {district} \"{m_NameSystem.GetRenderedLabelName(district)}\" to {selected} \"{m_NameSystem.GetRenderedLabelName(selected)}\". Re-open the building panel to verify.");
                    return;
                }
            }
            Mod.log.Info("Building already serves every district.");
        }
    }
}

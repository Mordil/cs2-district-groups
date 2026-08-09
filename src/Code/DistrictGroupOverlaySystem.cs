using Colossal.Mathematics;
using Game;
using Game.Common;
using Game.Input;
using Game.Rendering;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace multi_district_tool
{
    // Phase 4: draws each group's member-district boundaries in a distinct color
    // through the game's OverlayRenderSystem. Boundary segments are subdivided
    // and snapped to terrain/water height so they hug the ground like vanilla
    // district borders. Toggled by hotkey until the Phase 5 UI takes over.
    public partial class DistrictGroupOverlaySystem : GameSystemBase
    {
        private static readonly Color[] kPalette =
        {
            new Color(0.90f, 0.30f, 0.25f, 1f), // red
            new Color(0.25f, 0.55f, 0.95f, 1f), // blue
            new Color(0.30f, 0.80f, 0.40f, 1f), // green
            new Color(0.95f, 0.75f, 0.20f, 1f), // amber
            new Color(0.70f, 0.40f, 0.90f, 1f), // purple
            new Color(0.20f, 0.80f, 0.80f, 1f), // teal
            new Color(0.95f, 0.50f, 0.75f, 1f), // pink
            new Color(0.60f, 0.75f, 0.20f, 1f), // olive
            new Color(0.95f, 0.55f, 0.15f, 1f), // orange
            new Color(0.45f, 0.50f, 0.95f, 1f), // indigo
            new Color(0.55f, 0.35f, 0.20f, 1f), // brown
            new Color(0.55f, 0.60f, 0.65f, 1f), // slate
        };

        private const float kOutlineWidth = 5f;
        private const float kSegmentStep = 12f;

        private OverlayRenderSystem m_OverlayRenderSystem;
        private TerrainSystem m_TerrainSystem;
        private WaterSystem m_WaterSystem;
        private EntityQuery m_GroupQuery;
        private ProxyAction m_ToggleAction;
        private bool m_Visible;

        // The UI panel drives visibility too (panel open = overlay on).
        public void SetVisible(bool visible)
        {
            m_Visible = visible;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_TerrainSystem = World.GetOrCreateSystemManaged<TerrainSystem>();
            m_WaterSystem = World.GetOrCreateSystemManaged<WaterSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            m_ToggleAction = Mod.Settings?.GetAction(Setting.kOverlayToggleActionName);
            if (m_ToggleAction != null) m_ToggleAction.shouldBeEnabled = true;
        }

        protected override void OnUpdate()
        {
            if (m_ToggleAction?.WasPerformedThisFrame() ?? false)
            {
                m_Visible = !m_Visible;
                Mod.log.Info($"Group overlay {(m_Visible ? "ON" : "OFF")}");
            }
            if (!m_Visible || m_GroupQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            TerrainHeightData terrainHeight = m_TerrainSystem.GetHeightData();
            var waterSurface = m_WaterSystem.GetSurfaceData(out JobHandle _);

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle _);
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < groups.Length; i++)
            {
                Color color = kPalette[i % kPalette.Length];
                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(groups[i], isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    Entity district = member.m_District;
                    if (!EntityManager.Exists(district)
                        || EntityManager.HasComponent<Deleted>(district)
                        || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                    {
                        continue;
                    }

                    DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        float3 a = nodes[j].m_Position;
                        float3 b = nodes[(j + 1) % nodes.Length].m_Position;

                        // Subdivide so the border hugs terrain like vanilla.
                        int steps = math.max(1, (int)math.ceil(math.distance(a.xz, b.xz) / kSegmentStep));
                        float3 previous = a;
                        previous.y = WaterUtils.SampleHeight(ref waterSurface, ref terrainHeight, a);
                        for (int s = 1; s <= steps; s++)
                        {
                            float3 point = math.lerp(a, b, (float)s / steps);
                            point.y = WaterUtils.SampleHeight(ref waterSurface, ref terrainHeight, point);
                            buffer.DrawLine(color, new Line3.Segment(previous, point), kOutlineWidth);
                            previous = point;
                        }
                    }
                }
            }
        }
    }
}

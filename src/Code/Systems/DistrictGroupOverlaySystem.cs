using System.Reflection;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DistrictGroups
{
    // draws each group's member-district boundaries in its own intrinsic color through the game's OverlayRenderSystem.
    public partial class DistrictGroupOverlaySystem : GameSystemBase
    {
        // Wall-clock (not game-speed-affected) cadence for OnUpdate timing
        // samples. NegativeInfinity forces the first OnUpdate after the
        // overlay becomes visible to always sample, even if the user
        // dismisses the overlay again before kSampleIntervalSeconds elapses.
        private const float kSampleIntervalSeconds = 4f;
        private float m_LastSampleTime = float.NegativeInfinity;

        private static readonly PropertyInfo kRequireAreasProperty =
            typeof(ToolBaseSystem).GetProperty(nameof(ToolBaseSystem.requireAreas));

        private OverlayRenderSystem m_OverlayRenderSystem;
        private DefaultToolSystem m_DefaultToolSystem;
        private GameScreenUISystem m_GameScreenUISystem;
        private EntityQuery m_GroupQuery;
        private bool m_Visible;

        // Master on/off for the border+fill overlay. In-session only, like m_TypeFilter below -
        // resets to the default each time the game starts.
        private bool m_ShowOverlay = true;
        public bool ShowOverlay => m_ShowOverlay;

        private bool m_AreasVisible;
        public bool AreasVisible => m_AreasVisible;

        // -1 ("All Groups" in the panel) draws every group; otherwise only
        // groups of that type are drawn, mirroring the panel's own filtered list.
        private int m_TypeFilter = -1;

        private bool IsOverlayActive => m_Visible && m_ShowOverlay && !m_GameScreenUISystem.isMenuActive;

        // The UI panel drives visibility
        public void SetVisible(bool visible)
        {
            if (m_Visible == visible)
            {
                return;
            }
            bool wasActive = IsOverlayActive;
            m_Visible = visible;
            OnOverlayActiveChanged(wasActive);
            Mod.log.Info($"Group overlay toggled; visible:{m_Visible}");
            ApplyAreasVisibility();
        }

        // The panel's own "Show group overlay" checkbox.
        public void SetShowOverlay(bool show)
        {
            if (m_ShowOverlay == show)
            {
                return;
            }
            bool wasActive = IsOverlayActive;
            m_ShowOverlay = show;
            OnOverlayActiveChanged(wasActive);
            Mod.log.Info($"Show group overlay toggled; show:{m_ShowOverlay}");
        }

        private void OnOverlayActiveChanged(bool wasActive)
        {
            bool isActive = IsOverlayActive;
            if (isActive == wasActive)
            {
                return;
            }
            if (isActive)
            {
                m_LastSampleTime = float.NegativeInfinity;
            }
        }

        public void SetAreasVisible(bool visible)
        {
            if (m_AreasVisible == visible)
            {
                return;
            }
            m_AreasVisible = visible;
            Mod.log.Info($"District areas checkbox toggled; visible:{m_AreasVisible}");
            ApplyAreasVisibility();
        }

        private void ApplyAreasVisibility()
        {
            bool shouldShow = m_Visible && m_AreasVisible;
            kRequireAreasProperty?.SetValue(m_DefaultToolSystem,
                shouldShow ? AreaTypeMask.Districts : AreaTypeMask.None);
        }

        public void SetTypeFilter(int type)
        {
            Mod.log.Info($"Setting overlay filter; type:{type}");
            m_TypeFilter = type;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_GameScreenUISystem = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            ApplyAreasVisibility();
        }

        protected override void OnUpdate()
        {
            if (!IsOverlayActive || m_GroupQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            bool shouldSample = UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;
            System.Diagnostics.Stopwatch stopwatch = shouldSample ? System.Diagnostics.Stopwatch.StartNew() : null;

            float outlineWidth = Mod.Settings?.OverlayBorderWidth ?? Setting.kDefaultOverlayBorderWidth;

            OverlayRenderSystem.Buffer buffer = m_OverlayRenderSystem.GetBuffer(out JobHandle _);
            using NativeArray<Entity> groups = m_GroupQuery.ToEntityArray(Allocator.Temp);
            int districtCount = 0;
            int segmentCount = 0;
            for (int i = 0; i < groups.Length; i++)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                if (m_TypeFilter >= 0 && (int)data.m_Type != m_TypeFilter)
                {
                    continue;
                }

                Color color = data.m_Color;
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
                    districtCount++;

                    // roundness=(1,1) bakes rounded end caps into each
                    // segment's own quad, so adjacent segments' caps
                    // overlap exactly at the shared node and cover the
                    // corner notch without a separate draw call per node.
                    DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        float3 a = nodes[j].m_Position;
                        float3 b = nodes[(j + 1) % nodes.Length].m_Position;
                        buffer.DrawLine(
                            color,
                            color,
                            0f,
                            (OverlayRenderSystem.StyleFlags)0,
                            new Line3.Segment(a, b),
                            outlineWidth,
                            new float2(1f, 1f)
                        );
                        segmentCount++;
                    }
                }
            }

            if (shouldSample)
            {
                stopwatch.Stop();
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
                Mod.log.Info($"Overlay draw sample; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} group_count:{groups.Length} district_count:{districtCount} segment_count:{segmentCount}");
            }
        }
    }
}

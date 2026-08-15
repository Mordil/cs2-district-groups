using System.Reflection;
using Colossal.Mathematics;
using Game;
using Game.Areas;
using Game.Common;
using Game.Rendering;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DistrictGroups
{
    // draws each group's member-district boundaries in a distinct color through the game's OverlayRenderSystem.
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
        private EntityQuery m_GroupQuery;
        private bool m_Visible;

        // Master on/off for the border+fill overlay, persisted via Setting.ShowGroupOverlay
        private bool m_ShowOverlay;
        public bool ShowOverlay => m_ShowOverlay;

        private bool m_AreasVisible;
        public bool AreasVisible => m_AreasVisible;

        // -1 ("All Groups" in the panel) draws every group; otherwise only
        // groups of that type are drawn, mirroring the panel's own filtered list.
        private int m_TypeFilter = -1;


        private bool IsOverlayActive => m_Visible && m_ShowOverlay;

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
            if (Mod.Settings != null)
            {
                Mod.Settings.ShowGroupOverlay = show;
            }
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
            if (Mod.Settings != null)
            {
                Mod.Settings.DisplayDistrictAreas = visible;
            }
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
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());

            // Read the persisted checkbox state directly to avoid immediately rewriting the setting it just read
            m_AreasVisible = Mod.Settings?.DisplayDistrictAreas ?? false;
            m_ShowOverlay = Mod.Settings?.ShowGroupOverlay ?? true;
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
                if (m_TypeFilter >= 0
                    && (int)EntityManager.GetComponentData<DistrictGroupData>(groups[i]).m_Type != m_TypeFilter)
                {
                    continue;
                }

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
                    districtCount++;

                    // draw just a straight line between node points
                    DynamicBuffer<Game.Areas.Node> nodes = EntityManager.GetBuffer<Game.Areas.Node>(district, isReadOnly: true);
                    for (int j = 0; j < nodes.Length; j++)
                    {
                        float3 a = nodes[j].m_Position;
                        float3 b = nodes[(j + 1) % nodes.Length].m_Position;
                        buffer.DrawLine(color, new Line3.Segment(a, b), outlineWidth);
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

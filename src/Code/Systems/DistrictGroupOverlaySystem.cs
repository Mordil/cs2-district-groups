using System.Collections.Generic;
using System.Reflection;
using Colossal.Serialization.Entities;
using Game;
using Game.Areas;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DistrictGroups
{
    // draws each group's member-district boundaries and regions using its own intrinsic color
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
        private DistrictGroupSystem m_GroupSystem;
        private bool m_Visible;
        public bool Visible => m_Visible;

        // Used to detect when the vanilla area tool (which edits a district's boundaries) closes,
        // so the fill mesh can be forced to rebuild
        private ToolSystem m_ToolSystem;
        private AreaToolSystem m_AreaToolSystem;
        private bool m_WasAreaToolActive;
        public bool IsAreaToolActive => m_WasAreaToolActive;

        // Raw per-district colors of every visible group that claims that district (0..n colors each).
        // Rebuilt whenever the DistrictColors dirty bit is set.
        private readonly Dictionary<Entity, List<Color>> m_DistrictGroupColors = new Dictionary<Entity, List<Color>>();

        // What overlay-derived state needs rebuilding; bits are set by DetectChanges and
        // the explicit invalidation sites, cleared by each consumer after it rebuilds.
        [System.Flags]
        private enum OverlayDirtyFlags : byte
        {
            None = 0,
            DistrictColors = 1 << 0,   // m_DistrictGroupColors
            FillGeometry = 1 << 1,     // fill meshes (full rebuild)
            All = DistrictColors | FillGeometry,
        }

        private OverlayDirtyFlags m_DirtyFlags = OverlayDirtyFlags.All; // All = never built

        // Last GroupCompositionVersion already folded into m_DirtyFlags; advanced only
        // inside DetectChanges, so drift during inactive frames is caught on reactivation.
        private int m_LastSeenCompositionVersion = -1;

        // Master on/off for the border+fill overlay. In-session only, like m_TypeFilter below -
        // resets to the default each time the game starts.
        private bool m_ShowOverlay = true;
        public bool ShowOverlay => m_ShowOverlay;

        private bool m_AreasVisible;
        public bool AreasVisible => m_AreasVisible;

        // -1 ("All Groups" in the panel) draws every group; otherwise only
        // groups of that type are drawn, mirroring the panel's own filtered list.
        private int m_TypeFilter = -1;

        // Full-screen desaturation Volume shown alongside the border overlay
        private Volume m_DesaturationVolume;
        private ColorAdjustments m_ColorAdjustments;
        private bool m_DesaturationActive;

        private const float kFillVibrancy = 0.75f;
        private const float kMinFillSaturationPercent = 35f;

        // How many times the color cycle repeats across a multi-group district's fill.
        private const int kStripeRepeatCount = 4;

        // One entry per district with a fill mesh; keyed by district so a saturation-only change can
        // recolor an existing entry in place instead of destroying and recreating it.
        private struct FillEntry
        {
            public GameObject Object;
            public Mesh Mesh;
            public Texture2D Texture; // null for single-color districts (flat _UnlitColor tint, no texture)
            public MeshRenderer Renderer; // cached so recoloring doesn't need GetComponent
        }

        private GameObject m_FillRoot;
        private Material m_FillMaterial;
        private readonly Dictionary<Entity, FillEntry> m_FillEntries = new Dictionary<Entity, FillEntry>();

        // Last (clamped) saturation percent the fill meshes were actually built with
        // a Settings change has to be caught to trigger a rebuild
        private int m_FillBuiltSaturationPercent = -1;
        private bool m_FillActive;

        private bool IsOverlayActive => m_Visible && m_ShowOverlay && !m_GameScreenUISystem.isMenuActive;

        // +0.3 keeps the border/fill a hair above the terrain it's drawn on, on top of the user-tunable offset.
        private float OverlayHeightOffset =>
            (Mod.Settings?.OverlayBorderHeightOffset ?? Setting.kDefaultOverlayBorderHeightOffset) + 0.3f;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_GameScreenUISystem = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            ApplyAreasVisibility();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_AreaToolSystem = World.GetOrCreateSystemManaged<AreaToolSystem>();
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Combine(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
        }

        protected override void OnDestroy()
        {
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Remove(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
            DestroyDesaturationVolume();
            DestroyFillRoot();
            base.OnDestroy();
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);

            // We need to reset the overlay entirely in between cities because otherwise we'd display the last city's
            // cached overlay
            m_DirtyFlags = OverlayDirtyFlags.All;

            // the system doesn't get refreshed in between save-game loads, so we need to make sure we're in a clean enough state
            if (m_Visible)
            {
                m_Visible = false;
                ApplyAreasVisibility();
                Mod.log.Info("Forcing group overlay closed on load");
            }
            if (m_DesaturationActive)
            {
                DisableDesaturation();
            }
            if (m_FillActive)
            {
                DisableFill();
            }
        }

        protected override void OnUpdate()
        {
            // we don't want to flood the logs with rendering breadcrumbs, so we sample instead
            bool shouldSample = UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;
            if (shouldSample)
            {
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
            }

            bool active = IsOverlayActive && m_GroupSystem.HasGroups;
            if (active)
            {
                DetectChanges();
                EnsureDistrictGroupColors();
                UpdateDesaturation();
                UpdateFill();
                DrawGroupOverlays(shouldSample);
            }
            else
            {
                if (m_DesaturationActive)
                {
                    DisableDesaturation();
                }
                if (m_FillActive)
                {
                    DisableFill();
                }
            }
        }

        // Folds the upstream composition counter into dirty bits, consumers then only check/clear their own bit.
        private void DetectChanges()
        {
            int compositionVersion = m_GroupSystem.GroupCompositionVersion;
            if (compositionVersion != m_LastSeenCompositionVersion)
            {
                m_LastSeenCompositionVersion = compositionVersion;
                m_DirtyFlags |= OverlayDirtyFlags.All;
            }
        }

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
            if (m_TypeFilter == type)
            {
                Mod.log.Info($"Not changing overlay filter, same type; type:{type}");
                return;
            }

            Mod.log.Info($"Setting overlay filter; type:{type}");
            m_TypeFilter = type;

            // signal that the color cache and fill mesh both need to be rebuilt
            m_DirtyFlags |= OverlayDirtyFlags.All;
        }

        // Rebuilds the district <-> color list cache based on membership if it has been marked dirty
        private void EnsureDistrictGroupColors()
        {
            if ((m_DirtyFlags & OverlayDirtyFlags.DistrictColors) == 0)
            {
                return;
            }

            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

            m_DistrictGroupColors.Clear();

            using NativeArray<Entity> groups = m_GroupSystem.GetGroups(Allocator.Temp);
            for (int i = 0; i < groups.Length; i++)
            {
                DistrictGroupData data = EntityManager.GetComponentData<DistrictGroupData>(groups[i]);
                if (m_TypeFilter >= 0 && (int)data.m_Type != m_TypeFilter)
                {
                    continue;
                }

                DynamicBuffer<DistrictGroupMember> members = EntityManager.GetBuffer<DistrictGroupMember>(groups[i], isReadOnly: true);
                foreach (DistrictGroupMember member in members)
                {
                    Entity district = member.m_District;
                    if (!EntityManager.Exists(district) || !EntityManager.HasBuffer<Game.Areas.Node>(district))
                    {
                        continue;
                    }
                    if (!m_DistrictGroupColors.TryGetValue(district, out List<Color> colors))
                    {
                        colors = new List<Color>();
                        m_DistrictGroupColors[district] = colors;
                    }
                    colors.Add(data.m_Color);
                }
            }

            m_DirtyFlags &= ~OverlayDirtyFlags.DistrictColors;

            stopwatch.Stop();
            Mod.log.Debug($"Overlay district colors rebuilt; duration_ms:{stopwatch.Elapsed.TotalMilliseconds:F3} " +
                $"group_count:{groups.Length} district_count:{m_DistrictGroupColors.Count}");
        }

        // Tears down every runtime asset the overlay owns and forces the panel closed.
        public void RemoveAllData()
        {
            Mod.log.Info("Removing all group overlay state from the world");

            if (m_Visible)
            {
                m_Visible = false;
                ApplyAreasVisibility();
            }

            if (m_DesaturationVolume != null)
            {
                DestroyDesaturationVolume();
            }
            m_DesaturationActive = false;

            DestroyFillRoot();
            m_FillActive = false;

            m_DistrictGroupColors.Clear();
            m_DirtyFlags = OverlayDirtyFlags.All;

            Mod.log.Info("Finished removing all group overlay state from the world");
        }

        // Handle that the district area tool was closed
        private void OnActiveToolChanged(ToolBaseSystem tool)
        {
            bool isAreaToolActive = tool == m_AreaToolSystem;
            if (m_WasAreaToolActive && !isAreaToolActive)
            {
                Mod.log.Info("Area tool closed, forcing fill rebuild");
                m_DirtyFlags |= OverlayDirtyFlags.FillGeometry;
            }
            m_WasAreaToolActive = isAreaToolActive;
        }
    }
}

using System.Collections.Generic;
using System.Reflection;
using Game;
using Game.Areas;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
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
        private EntityQuery m_GroupQuery;
        private bool m_Visible;

        // Used to detect when the vanilla area tool (which edits a district's boundaries) closes,
        // so the fill mesh can be forced to rebuild
        private ToolSystem m_ToolSystem;
        private AreaToolSystem m_AreaToolSystem;
        private bool m_WasAreaToolActive;
        public bool IsAreaToolActive => m_WasAreaToolActive;

        // borders draw on every frame, so we cache data infrequently changing data
        // rebuilt when m_GroupSystem.Version or m_TypeFilter changes
        private readonly Dictionary<Entity, Color> m_DistrictColorCache = new Dictionary<Entity, Color>();
        private int m_ColorCacheVersion = -1;

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

        private GameObject m_FillRoot;
        private Material m_FillMaterial;
        private readonly List<GameObject> m_FillObjects = new List<GameObject>();
        private readonly List<Mesh> m_FillMeshes = new List<Mesh>();
        private readonly List<Texture2D> m_FillTextures = new List<Texture2D>();
        private int m_FillBuiltVersion = -1;

        // Last (clamped) saturation percent the fill meshes were actually built with
        // a Settings change has to be caught to trigger a rebuild
        private int m_FillBuiltSaturationPercent = -1;
        private bool m_FillActive;

        private bool IsOverlayActive => m_Visible && m_ShowOverlay && !m_GameScreenUISystem.isMenuActive;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_OverlayRenderSystem = World.GetOrCreateSystemManaged<OverlayRenderSystem>();
            m_DefaultToolSystem = World.GetOrCreateSystemManaged<DefaultToolSystem>();
            m_GameScreenUISystem = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_GroupSystem = World.GetOrCreateSystemManaged<DistrictGroupSystem>();
            m_GroupQuery = GetEntityQuery(ComponentType.ReadOnly<DistrictGroupData>());
            ApplyAreasVisibility();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_AreaToolSystem = World.GetOrCreateSystemManaged<AreaToolSystem>();
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Combine(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
        }

        protected override void OnUpdate()
        {
            // we don't want to flood the logs with rendering breadcrumbs, so we sample instead
            bool shouldSample = UnityEngine.Time.realtimeSinceStartup - m_LastSampleTime >= kSampleIntervalSeconds;
            if (shouldSample)
            {
                m_LastSampleTime = UnityEngine.Time.realtimeSinceStartup;
            }

            bool active = IsOverlayActive && !m_GroupQuery.IsEmptyIgnoreFilter;
            if (active)
            {
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

        protected override void OnDestroy()
        {
            m_ToolSystem.EventToolChanged = (System.Action<ToolBaseSystem>)System.Delegate.Remove(
                m_ToolSystem.EventToolChanged, (System.Action<ToolBaseSystem>)OnActiveToolChanged);
            DestroyDesaturationVolume();
            DestroyFillRoot();
            base.OnDestroy();
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
            m_ColorCacheVersion = -1;
            m_FillBuiltVersion = -1;
        }

        // Handle that the district area tool was closed
        private void OnActiveToolChanged(ToolBaseSystem tool)
        {
            bool isAreaToolActive = tool == m_AreaToolSystem;
            if (m_WasAreaToolActive && !isAreaToolActive)
            {
                Mod.log.Info("Area tool closed, forcing fill rebuild");
                m_FillBuiltVersion = -1;
            }
            m_WasAreaToolActive = isAreaToolActive;
        }
    }
}

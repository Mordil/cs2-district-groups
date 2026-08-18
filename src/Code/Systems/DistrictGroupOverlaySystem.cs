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
        private DistrictGroupSystem m_GroupSystem;
        private EntityQuery m_GroupQuery;
        private bool m_Visible;

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
        }

        protected override void OnUpdate()
        {
            bool active = IsOverlayActive && !m_GroupQuery.IsEmptyIgnoreFilter;
            if (active)
            {
                UpdateDesaturation();
                DrawGroupOverlays();
            }
            else if (m_DesaturationActive)
            {
                DisableDesaturation();
            }
        }

        protected override void OnDestroy()
        {
            DestroyDesaturationVolume();
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

            // signal that the color cache needs to be rebuilt
            m_ColorCacheVersion = -1;
        }
    }
}

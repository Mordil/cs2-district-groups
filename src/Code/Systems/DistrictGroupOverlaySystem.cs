using System.Collections.Generic;
using System.Reflection;
using Game;
using Game.Areas;
using Game.Rendering;
using Game.Tools;
using Game.UI.InGame;
using Unity.Entities;
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
            if (!IsOverlayActive || m_GroupQuery.IsEmptyIgnoreFilter)
            {
                return;
            }

            DrawGroupOverlays();
        }
    }
}

using Game.Areas;

namespace DistrictGroups
{
    public partial class DistrictGroupOverlaySystem
    {
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
    }
}

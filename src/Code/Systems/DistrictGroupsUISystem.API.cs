using Unity.Entities;

namespace DistrictGroups
{
    public partial class DistrictGroupsUISystem
    {
        // Lets the React UI route its own logs into the mod's log file
        // alongside C# logs, tagged so their origin is obvious.
        private static void LogFromUI(string level, string message)
        {
            string tagged = $"[UI] {message}";
            switch (level)
            {
                case "debug": Mod.log.Debug(tagged); break;
                case "warn": Mod.log.Warn(tagged); break;
                case "error": Mod.log.Error(tagged); break;
                case "critical": Mod.log.Critical(tagged); break;
                default: Mod.log.Info(tagged); break;
            }
        }

        // "setOverlay" fires exactly at our panel's open/close, so it doubles
        // as the signal for closing/restoring the vanilla selected-info panel.
        private void OnPanelOpenChanged(bool open)
        {
            m_OverlaySystem.SetVisible(open);

            if (open)
            {
                m_SavedSelection = m_SelectedInfoUISystem.selectedEntity;
                if (m_SavedSelection != Entity.Null)
                {
                    m_SelectedInfoUISystem.SetSelection(Entity.Null);
                }
            }
            else
            {
                if (m_SavedSelection != Entity.Null && EntityManager.Exists(m_SavedSelection))
                {
                    m_SelectedInfoUISystem.SetSelection(m_SavedSelection);
                }
                m_SavedSelection = Entity.Null;
            }
        }
    }
}

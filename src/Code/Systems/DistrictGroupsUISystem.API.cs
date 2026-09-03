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

        // "setOverlay" fires exactly at our panel's open/close, so it also signals to close something competing for the screen
        private void OnPanelOpenChanged(bool open)
        {
            m_OverlaySystem.SetVisible(open);

            if (open)
            {
                // Our panel and an active tool can't share the screen
                // the panel is what the player just asked for - so cancel the tool
                // Picking a tool again dismisses our panel
                if (m_ToolSystem.activeTool != null && m_ToolSystem.activeTool != m_DefaultToolSystem)
                {
                    Mod.log.Info($"Cancelling active tool for panel open; tool:{m_ToolSystem.activeTool.toolID}");
                    m_ToolSystem.activeTool = m_DefaultToolSystem;
                }

                m_SavedSelection = m_SelectedInfoUISystem.selectedEntity;
                if (m_SavedSelection != Entity.Null)
                {
                    m_SelectedInfoUISystem.SetSelection(Entity.Null);
                }
            }
            else
            {
                // Selecting something else is one of the things that dismisses our panel, so by the
                // time we get here the player may already have a newer selection - theirs wins.
                if (m_SavedSelection != Entity.Null
                    && m_SelectedInfoUISystem.selectedEntity == Entity.Null
                    && EntityManager.Exists(m_SavedSelection))
                {
                    m_SelectedInfoUISystem.SetSelection(m_SavedSelection);
                }
                m_SavedSelection = Entity.Null;
            }
        }
    }
}

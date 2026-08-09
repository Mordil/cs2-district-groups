using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.IO.AssetDatabase;

namespace multi_district_tool
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(multi_district_tool)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting Settings { get; private set; }
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            m_Setting.RegisterKeyBindings();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(multi_district_tool), m_Setting, new Setting(this));
            Settings = m_Setting;

            updateSystem.UpdateAt<ProbeSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<DistrictGroupSyncSystem>(SystemUpdatePhase.Modification5);
            updateSystem.UpdateAt<DistrictGroupOverlaySystem>(SystemUpdatePhase.ToolUpdate);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}

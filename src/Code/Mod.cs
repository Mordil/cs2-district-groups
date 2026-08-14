using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.IO.AssetDatabase;
using Colossal.UI;
using System.IO;

namespace DistrictGroups
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(DistrictGroups)}").SetShowsErrorsInUI(false);

        // The mod's own coui:// host, serving Icons/*.svg to the UI as
        // coui://districtgroups/<Name>.svg. Same pattern Unified Icon Library uses
        // for coui://uil/. Keep in sync with kIconHost in src/UI/src/mods/modIcons.tsx.
        public const string kIconHost = "districtgroups";

        public static Setting Settings { get; private set; }
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"{nameof(OnLoad)};");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info($"Resolved mod asset; path:{asset.path}");
                RegisterIconHost(asset.path);
            }
            else
            {
                log.Warn("Could not resolve mod asset, custom icons will not load;");
            }

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));


            AssetDatabase.global.LoadSettings(nameof(DistrictGroups), m_Setting, new Setting(this));
            Settings = m_Setting;

            updateSystem.UpdateAt<DistrictGroupSyncSystem>(SystemUpdatePhase.Modification5);
            updateSystem.UpdateAt<DistrictGroupOverlaySystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<DistrictGroupSelectionSystem>(SystemUpdatePhase.ToolUpdate);
            updateSystem.UpdateAt<DistrictGroupsUISystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<DistrictGroupSection>(SystemUpdatePhase.UIUpdate);
        }

        // The Icons folder sits next to the deployed DLL - the csproj's DeployIcons
        // target re-copies it after every deploy, since the deploy step wipes the
        // mod folder. Missing folder is not fatal: the UI just renders no image.
        private static void RegisterIconHost(string assetPath)
        {
            string modDir = Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(modDir))
            {
                log.Warn($"Could not resolve mod directory, custom icons will not load; asset_path:{assetPath}");
                return;
            }

            string iconDir = modDir + "/Icons/";
            UIManager.defaultUISystem.AddHostLocation(kIconHost, iconDir, shouldWatch: true, priority: 0);
            log.Info($"Registered icon host; host:{kIconHost} path:{iconDir} exists:{Directory.Exists(iconDir)}");
        }

        public void OnDispose()
        {
            log.Info($"{nameof(OnDispose)};");

            UIManager.defaultUISystem?.RemoveHostLocation(kIconHost);

            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
        }
    }
}

using System.IO;
using System.Reflection;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.IO.AssetDatabase;
using Colossal.UI;

namespace DistrictGroups
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(DistrictGroups)}").SetShowsErrorsInUI(false);

        // The mod's own coui:// host, serving Icons/*.svg to the UI as coui://districtgroups/<Name>.svg.
        public const string kIconHost = "districtgroups";

        // Read from the csproj's <Version> via the SDK-generated assembly attribute, so it never drifts out of sync with the built DLL.
        public static string Version
        {
            get
            {
                string version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                    ?? "unknown";
                int shaIndex = version.IndexOf('+');
                return shaIndex >= 0 ? version.Substring(0, shaIndex) : version;
            }
        }

        public static Setting Settings { get; private set; }
        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info($"{nameof(OnLoad)}; version:{Version}");

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info($"Resolved mod asset; path:{asset.path}");
                RegisterIconHost(asset.path);
            }
            else
            {
                log.Error("Could not resolve mod asset, custom icons will not load;");
            }

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            GameManager.instance.localizationManager.AddSource("de-DE", new LocaleDE(m_Setting));
            GameManager.instance.localizationManager.AddSource("es-ES", new LocaleES(m_Setting));
            GameManager.instance.localizationManager.AddSource("fr-FR", new LocaleFR(m_Setting));
            GameManager.instance.localizationManager.AddSource("it-IT", new LocaleIT(m_Setting));
            GameManager.instance.localizationManager.AddSource("ja-JP", new LocaleJA(m_Setting));
            GameManager.instance.localizationManager.AddSource("ko-KR", new LocaleKO(m_Setting));
            GameManager.instance.localizationManager.AddSource("pl-PL", new LocalePL(m_Setting));
            GameManager.instance.localizationManager.AddSource("pt-BR", new LocalePTBR(m_Setting));
            GameManager.instance.localizationManager.AddSource("ru-RU", new LocaleRU(m_Setting));
            GameManager.instance.localizationManager.AddSource("zh-HANS", new LocaleZHHANS(m_Setting));
            GameManager.instance.localizationManager.AddSource("zh-HANT", new LocaleZHHANT(m_Setting));


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
                log.Error($"Could not resolve mod directory, custom icons will not load; asset_path:{assetPath}");
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

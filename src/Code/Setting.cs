using System;
using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Unity.Entities;
using UnityEngine.Device;

namespace DistrictGroups
{
    [FileLocation(nameof(DistrictGroups))]
    [SettingsUIGroupOrder(kSectionDefault, kSectionDebug, kSectionVersion)]
    [SettingsUIShowGroupName(kSectionDebug)]
    public class Setting : ModSetting
    {
        public const string kTabGeneral = "General";
        public const string kTabDeveloper = "Developer";

        public const string kSectionDefault = "Default";
        public const string kSectionDebug = "Debug";
        public const string kSectionVersion = "Version";

        private const string kIssueUrl = "https://github.com/Mordil/cs2-district-groups/issues/new";

        public const int kDefaultOverlayBorderWidth = 15;

        // There's nothing in memory to dump while sitting on the main menu.
        public bool IsNotInGame() => GameManager.instance.gameMode != GameMode.Game;

        private bool m_DisplayDistrictAreas;
        private bool m_ShowGroupOverlay;
        private bool m_ShowFpsCounter;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            m_DisplayDistrictAreas = false;
            m_ShowGroupOverlay = true;
            m_ShowFpsCounter = false;
            OverlayBorderWidth = kDefaultOverlayBorderWidth;
        }

        // Persisted, but controlled via in-game mod UI
        [SettingsUIHidden]
        public bool DisplayDistrictAreas
        {
            get => m_DisplayDistrictAreas;
            set
            {
                m_DisplayDistrictAreas = value;
                ApplyAndSave();
            }
        }

        // Persisted, but controlled via in-game mod UI
        [SettingsUIHidden]
        public bool ShowGroupOverlay
        {
            get => m_ShowGroupOverlay;
            set
            {
                m_ShowGroupOverlay = value;
                ApplyAndSave();
            }
        }

        // Width (in world units) of the group boundary overlay lines drawn by the overlay system.
        [SettingsUISlider(min = 5, max = 50, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kTabGeneral, kSectionDefault)]
        public int OverlayBorderWidth { get; set; }

        // Writes group/building/district counts and a full per-group breakdown to the log file, for troubleshooting reports.
        [SettingsUIButton]
        [SettingsUISection(kTabGeneral, kSectionDebug)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsNotInGame))]
        public bool DumpDebugData
        {
            set => TryDumpDebugData();
        }

        // Captures a dump alongside the report so it's already in the log by the time the player pastes it into the issue.
        private void TryDumpDebugData()
        {
            Mod.log.Info($"Attempting to dump debug data; in_game:{!IsNotInGame()}");

            World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<DistrictGroupSystem>()?
                .DumpDebugData();
        }

        // Opens the mod's GitHub issue tracker in the player's default browser.
        [SettingsUIButton]
        [SettingsUISection(kTabGeneral, kSectionDebug)]
        public bool FileBug
        {
            set
            {
                Mod.log.Info("FileBug Settings button clicked");

                TryDumpDebugData();

                try
                {
                    Application.OpenURL(kIssueUrl);
                    Mod.log.Info("Issue tracker opened successfully");
                }
                catch (Exception e)
                {
                    Mod.log.Warn($"Could not open issue tracker URL; url:{kIssueUrl} error:{e.Message}");
                }
            }
        }

        // Read-only, so players can confirm which build they're on when reporting issues.
        [SettingsUISection(kTabGeneral, kSectionVersion)]
        public string ModVersion => Mod.Version;

        #region DEVELOPER
        // Hardcoded false outside a Debug build
        [SettingsUISection(kTabDeveloper, kSectionDefault)]
        public bool ShowFpsCounter
        {
            get =>
#if DEBUG
                m_ShowFpsCounter;
#else
                false;
#endif
            set
            {
                m_ShowFpsCounter = value;
                ApplyAndSave();
            }
        }
        #endregion
    }
}

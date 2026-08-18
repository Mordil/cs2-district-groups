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
    [SettingsUIGroupOrder(kSectionDefault, kSectionOverlay, kSectionDebug, kSectionVersion)]
    [SettingsUIShowGroupName(kSectionDebug)]
    public class Setting : ModSetting
    {
        public const string kTabGeneral = "General";
        public const string kTabDeveloper = "Developer";

        public const string kSectionDefault = "Default";
        public const string kSectionOverlay = "Overlay";
        public const string kSectionDebug = "Debug";
        public const string kSectionVersion = "Version";

        private const string kIssueUrl = "https://github.com/Mordil/cs2-district-groups/issues/new";

        public const int kDefaultOverlayBorderWidth = 15;
        public const int kDefaultOverlayBorderAlpha = 100;
        public const int kDefaultOverlayBorderHeightOffset = 15;

        // There's nothing in memory to dump while sitting on the main menu.
        public bool IsNotInGame() => GameManager.instance.gameMode != GameMode.Game;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            OverlayBorderWidth = kDefaultOverlayBorderWidth;
            OverlayBorderAlpha = kDefaultOverlayBorderAlpha;
            OverlayBorderHeightOffset = kDefaultOverlayBorderHeightOffset;
        }

        // Height (in world units) the group boundary overlay lines are raised above the district node height.
        [SettingsUISlider(min = 0, max = 250, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kTabGeneral, kSectionOverlay)]
        public int OverlayBorderHeightOffset { get; set; }

        // Opacity of the group boundary overlay lines drawn by the overlay system; 0 is fully transparent, 100 fully opaque.
        [SettingsUISlider(min = 0, max = 100, step = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kTabGeneral, kSectionOverlay)]
        public int OverlayBorderAlpha { get; set; }

        // Width (in world units) of the group boundary overlay lines drawn by the overlay system.
        [SettingsUISlider(min = 5, max = 50, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kTabGeneral, kSectionOverlay)]
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
#if DEBUG
#else
/*
    because settings are reflection based, properties need to be stripped in release or each annotation wrapped in #if DEBUG,
    but because code still references these properties, we need to provide constants
*/
#endif
        #endregion
    }
}

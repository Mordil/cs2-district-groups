using System;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
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
    [SettingsUITabOrder(kTabGeneral, kTabOverlay, kTabDeveloper)]
    [SettingsUIGroupOrder(
        kSectionDefault,
        kSectionOverlayBorder, kSectionOverlayFill,
        kSectionDebug, kSectionVersion
    )]
    [SettingsUIShowGroupName(kSectionDebug)]
    public class Setting : ModSetting
    {
        public const string kTabGeneral = "General";
        public const string kTabOverlay = "Overlay";
        public const string kTabDeveloper = "Developer";

        public const string kSectionDefault = "Default";
        public const string kSectionOverlayBorder = "OverlayBorder";
        public const string kSectionOverlayFill = "OverlayFill";
        public const string kSectionDebug = "Debug";
        public const string kSectionVersion = "Version";

        private const string kIssueUrl = "https://github.com/Mordil/cs2-district-groups/issues/new";

        public const int kDefaultOverlayBorderWidth = 15;
        public const int kDefaultOverlayBorderAlpha = 100;
        public const int kDefaultOverlayDesaturationPercent = 80;
        public const int kDefaultOverlayFillSaturationPercent = 60;
        public const bool kDefaultOverlayFillUseTransparency = true;
        public const int kDefaultRefreshRateSeconds = 10;
#if DEBUG
        public const bool kDefaultEnableDebugLogging = true;
#else
        public const bool kDefaultEnableDebugLogging = false;
#endif

        private bool m_EnableDebugLogging;

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
            OverlayDesaturationPercent = kDefaultOverlayDesaturationPercent;
            OverlayFillSaturationPercent = kDefaultOverlayFillSaturationPercent;
            OverlayFillUseTransparency = kDefaultOverlayFillUseTransparency;
            RefreshRateSeconds = kDefaultRefreshRateSeconds;
            EnableDebugLogging = kDefaultEnableDebugLogging;
        }

        // How much the rest of the scene is desaturated while the group overlay is visible
        [SettingsUISlider(min = 0, max = 100, step = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kTabOverlay, kSectionDefault)]
        public int OverlayDesaturationPercent { get; set; }

        // Opacity of the group boundary overlay lines drawn by the overlay system; 0 is fully transparent, 100 fully opaque.
        [SettingsUISlider(min = 0, max = 100, step = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kTabOverlay, kSectionOverlayBorder)]
        public int OverlayBorderAlpha { get; set; }

        // Width (in world units) of the group boundary overlay lines drawn by the overlay system.
        [SettingsUISlider(min = 5, max = 50, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kTabOverlay, kSectionOverlayBorder)]
        public int OverlayBorderWidth { get; set; }

        // Whether the group fill mesh/texture renders with transparency (blended over the scene) or as a solid, fully opaque color.
        [SettingsUISection(kTabOverlay, kSectionOverlayFill)]
        public bool OverlayFillUseTransparency { get; set; }

        // How saturated the group fill color is. The slider spans the full 0-100% range, but the value actually between 35-100%, scaled.
        [SettingsUISlider(min = 0, max = 100, step = 1, unit = Unit.kPercentage)]
        [SettingsUISection(kTabOverlay, kSectionOverlayFill)]
        public int OverlayFillSaturationPercent { get; set; }

        // How often the mod recomputes aggregate district information
        [SettingsUISlider(min = 1, max = 30, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kTabGeneral, kSectionDefault)]
        public int RefreshRateSeconds { get; set; }

        // Resets every mod setting back to its shipped default.
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kTabGeneral, kSectionDefault)]
        public bool ResetSettings
        {
            set
            {
                Mod.log.Info("ResetSettings button clicked");

                SetDefaults();
                ApplyAndSave();
            }
        }

        // Gates the log's own minimum severity, so disabling this stops Debug-level entries at the source instead of just hiding them from a report.
        [SettingsUISection(kTabGeneral, kSectionDebug)]
        public bool EnableDebugLogging
        {
            get => m_EnableDebugLogging;
            set
            {
                m_EnableDebugLogging = value;
                Mod.log.effectivenessLevel = value ? Level.Debug : Level.Info;
            }
        }

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
                    Mod.log.Error($"Could not open issue tracker URL; url:{kIssueUrl} error:{e.Message}");
                }
            }
        }

        // Read-only, so players can confirm which build they're on when reporting issues.
        [SettingsUISection(kTabGeneral, kSectionVersion)]
        public string ModVersion => Mod.Version;

        // Read-only, so players can tell a shipped Release build apart from a Debug one.
        [SettingsUISection(kTabGeneral, kSectionVersion)]
        public string ReleaseChannel =>
#if DEBUG
            "Debug";
#else
            "Release";
#endif

        // Wipes every group, building assignment, and overlay asset the mod has added to the current save.
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kTabGeneral, kSectionVersion)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsNotInGame))]
        public bool RemoveModData
        {
            set => TryRemoveModData();
        }

        private void TryRemoveModData()
        {
            Mod.log.Info("RemoveModData button clicked");

            World world = World.DefaultGameObjectInjectionWorld;
            world?.GetExistingSystemManaged<DistrictGroupSystem>()?.RemoveAllData();
            world?.GetExistingSystemManaged<DistrictGroupOverlaySystem>()?.RemoveAllData();

            Mod.log.Info("Finished removing all mod data");
        }

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

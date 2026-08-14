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
    [SettingsUIGroupOrder(kGeneralGroup, kDebugGroup, kVersionGroup)]
    [SettingsUIShowGroupName(kGeneralGroup, kDebugGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kDebugGroup = "Debug";
        public const string kVersionGroup = "Version";

        private const string kIssueUrl = "https://github.com/Mordil/cs2-district-groups/issues/new";

        public const int kMinOverlayBorderWidth = 5;
        public const int kMaxOverlayBorderWidth = 50;
        public const int kDefaultOverlayBorderWidth = 15;

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            m_DisplayDistrictAreas = false;
            OverlayBorderWidth = kDefaultOverlayBorderWidth;
        }

        private bool m_DisplayDistrictAreas;

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

        // Width (in world units) of the group boundary overlay lines drawn by the overlay system.
        [SettingsUISlider(min = kMinOverlayBorderWidth, max = kMaxOverlayBorderWidth, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kGeneralGroup)]
        public int OverlayBorderWidth { get; set; }

        // Writes group/building/district counts and a full per-group
        // breakdown to the log file, for troubleshooting reports.
        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
        [SettingsUIHideByCondition(typeof(Setting), nameof(IsNotInGame))]
        public bool DumpDebugData
        {
            set => TryDumpDebugData();
        }

        // There's nothing in memory to dump while sitting on the main menu.
        public bool IsNotInGame() => GameManager.instance.gameMode != GameMode.Game;

        // Captures a dump alongside the report so it's already in the log by
        // the time the player pastes it into the issue. No-ops from the main
        // menu, same as the dump button being hidden there.
        private void TryDumpDebugData()
        {
            Mod.log.Info($"Attempting to dump debug data; in_game:{!IsNotInGame()}");

            World.DefaultGameObjectInjectionWorld?
                .GetExistingSystemManaged<DistrictGroupSystem>()?
                .DumpDebugData();
        }

        // Opens the mod's GitHub issue tracker in the player's default browser.
        [SettingsUIButton]
        [SettingsUISection(kSection, kDebugGroup)]
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
        [SettingsUISection(kSection, kVersionGroup)]
        public string ModVersion => Mod.Version;
    }
}

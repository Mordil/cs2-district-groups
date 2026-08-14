using Colossal.IO.AssetDatabase;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI;
using Unity.Entities;

namespace DistrictGroups
{
    [FileLocation(nameof(DistrictGroups))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kDebugGroup = "Debug";

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
            set
            {
                World.DefaultGameObjectInjectionWorld?
                    .GetExistingSystemManaged<DistrictGroupSystem>()?
                    .DumpDebugData();
            }
        }

        // There's nothing in memory to dump while sitting on the main menu.
        public bool IsNotInGame() => GameManager.instance.gameMode != GameMode.Game;
    }
}

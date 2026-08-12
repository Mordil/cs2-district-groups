using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;
using System.Collections.Generic;

namespace DistrictGroups
{
    [FileLocation(nameof(DistrictGroups))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";

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

        // Persisted (not shown in the options UI — controlled by the panel's own
        // checkbox instead). The setter saves immediately so the choice survives
        // a session even if the game isn't closed cleanly.
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

        // Width (in world units) of the group boundary overlay lines drawn by
        // DistrictGroupOverlaySystem. Read fresh every frame there, so this
        // just needs the options UI's own apply/save flow - no custom setter.
        [SettingsUISlider(min = kMinOverlayBorderWidth, max = kMaxOverlayBorderWidth, step = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kGeneralGroup)]
        public int OverlayBorderWidth { get; set; }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "District Groups" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kGeneralGroup), "General" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Overlay border width" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Width of the colored district-group boundary lines drawn on the map." },
            };
        }

        public void Unload()
        {
        }
    }
}

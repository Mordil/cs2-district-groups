using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;

namespace DistrictGroups
{
    [FileLocation(nameof(DistrictGroups))]
    public class Setting : ModSetting
    {
        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        public override void SetDefaults()
        {
            m_DisplayDistrictAreas = false;
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
            };
        }

        public void Unload()
        {
        }
    }
}

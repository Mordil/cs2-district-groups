using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;

namespace DistrictGroups
{
    [FileLocation(nameof(DistrictGroups))]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kGeneralGroup = "General";
        public const string kBindingGroup = "Keybindings";

        public const string kDumpActionName = "DumpDistrictsAction";
        public const string kWriteActionName = "WriteProbeAction";
        public const string kGroupTestActionName = "GroupTestAction";
        public const string kUnassignActionName = "UnassignGroupAction";
        public const string kOverlayToggleActionName = "OverlayToggleAction";

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

        [SettingsUIButton]
        [SettingsUISection(kSection, kGeneralGroup)]
        public bool DumpDistrictsButton { set { ProbeSystem.DumpRequested = true; } }

        [SettingsUIButton]
        [SettingsUISection(kSection, kGeneralGroup)]
        public bool WriteProbeButton { set { ProbeSystem.WriteRequested = true; } }

        [SettingsUIKeyboardBinding(BindingKeyboard.D, kDumpActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kBindingGroup)]
        public ProxyBinding DumpDistrictsBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.W, kWriteActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kBindingGroup)]
        public ProxyBinding WriteProbeBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.G, kGroupTestActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kBindingGroup)]
        public ProxyBinding GroupTestBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.U, kUnassignActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kBindingGroup)]
        public ProxyBinding UnassignGroupBinding { get; set; }

        [SettingsUIKeyboardBinding(BindingKeyboard.O, kOverlayToggleActionName, ctrl: true, shift: true)]
        [SettingsUISection(kSection, kBindingGroup)]
        public ProxyBinding OverlayToggleBinding { get; set; }
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

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDistrictsButton)), "Probe: dump districts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDistrictsButton)), "Logs all districts plus the selected entity's district data to the mod log. Load a city, select a building, then click." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.WriteProbeButton)), "Probe: add district to selected building" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.WriteProbeButton)), "Adds the first unserved district to the selected service building's ServiceDistrict buffer. Select a service building (police, school, ...) first." },

                { m_Setting.GetOptionGroupLocaleID(Setting.kBindingGroup), "Keybindings" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDistrictsBinding)), "Dump districts (in-game)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDistrictsBinding)), "Hotkey: log all districts plus the selected building's district data. Default Ctrl+Shift+D." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.WriteProbeBinding)), "Add district to selected building (in-game)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.WriteProbeBinding)), "Hotkey: add the first unserved district to the selected service building. Default Ctrl+Shift+W." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.GroupTestBinding)), "Create test group + assign (in-game)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.GroupTestBinding)), "Hotkey: create 'Test Group' with the first two districts and assign it to the selected service building. Default Ctrl+Shift+G." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UnassignGroupBinding)), "Unassign group from building (in-game)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UnassignGroupBinding)), "Hotkey: remove the group assignment from the selected building (serves the whole city again). Default Ctrl+Shift+U." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayToggleBinding)), "Toggle group overlay (in-game)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayToggleBinding)), "Hotkey: show/hide colored district-group boundaries on the map. Default Ctrl+Shift+O." },

                { m_Setting.GetBindingMapLocaleID(), "District Groups" },
            };
        }

        public void Unload()
        {
        }
    }
}

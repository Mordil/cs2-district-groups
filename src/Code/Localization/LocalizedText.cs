using Colossal.Localization;
using Game.SceneFlow;

namespace DistrictGroups
{
    public static class LocalizedText
    {
        // Placeholder values are replaced by the callers.
        public static string Get(string localeId, string fallback)
        {
            LocalizationDictionary dictionary = GameManager.instance?.localizationManager?.activeDictionary;

            if (dictionary != null
                && dictionary.TryGetValue(localeId, out string value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }

            Mod.log.Warn($"No active locale entry, using fallback text; locale_id:{localeId}");
            return fallback;
        }
    }
}

using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleZHHANS : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { LocalizationKey.OperatingDistrictsLabel, "工作区" },
            };
        }

        public void Unload() { }
    }
}

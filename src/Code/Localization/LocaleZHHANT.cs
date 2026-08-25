using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleZHHANT : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { LocalizationKey.OperatingDistrictsLabel, "操作區" },
            };
        }

        public void Unload() { }
    }
}

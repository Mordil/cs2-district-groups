using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleJA : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { LocalizationKey.OperatingDistrictsLabel, "稼働特区" },
            };
        }

        public void Unload() { }
    }
}

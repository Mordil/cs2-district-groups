using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleES : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { LocalizationKey.OperatingDistrictsLabel, "Distritos operativos" },
            };
        }

        public void Unload() { }
    }
}

using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocalePL : IDictionarySource
    {
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { LocalizationKey.OperatingDistrictsLabel, "Funkcjonujące dzielnice" },
            };
        }

        public void Unload() { }
    }
}

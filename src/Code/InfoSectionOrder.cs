using System.Collections.Generic;
using System.Reflection;
using Game.UI.InGame;
using Unity.Entities;

namespace DistrictGroups
{
    // Places a mod info-panel section at a chosen spot in the vanilla section order.
    internal static class InfoSectionOrder
    {
        private static readonly FieldInfo kMiddleSectionsField =
            typeof(SelectedInfoUISystem).GetField("m_MiddleSections", BindingFlags.NonPublic | BindingFlags.Instance);

        // Inserts the section directly above the named vanilla section, appending it to the panel when that section cannot be found.
        internal static void InsertBefore<TAnchor>(SelectedInfoUISystem infoUISystem, ISectionSource section)
            where TAnchor : ComponentSystemBase, ISectionSource
        {
            TAnchor anchor = infoUISystem.World.GetOrCreateSystemManaged<TAnchor>();
            if (kMiddleSectionsField?.GetValue(infoUISystem) is List<ISectionSource> sections)
            {
                int index = sections.IndexOf(anchor);
                if (index >= 0)
                {
                    sections.Insert(index, section);
                    return;
                }
            }

            Mod.log.Warn($"Could not locate the anchor section in the info panel, appending our section instead; anchor:{typeof(TAnchor).Name}");
            infoUISystem.AddMiddleSection(section);
        }
    }
}

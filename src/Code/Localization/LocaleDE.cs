using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleDE : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleDE(Setting setting)
        {
            m_Setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            /*
                Key strings for the custom mod UI must match locale.ts's id() exactly
            */

            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "District Groups" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Allgemein" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Overlay" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Entwickler" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Standard" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Fehlerbehebung" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Randbreite" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Breite der Grenzlinien der Bezirke, die auf dem Overlay gezeichnet werden." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "Randtransparenz" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "Transparenz der Grenzlinien der Bezirke, die auf dem Overlay gezeichnet werden.\n\n0% ist vollständig deckend, 100% ist vollständig transparent." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Szenenentsättigung" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Wie stark der Rest der Szene entsättigt wird, während das Gruppen-Overlay sichtbar ist.\n\n0% lässt die Szene unverändert, 100% ergibt eine Graustufendarstellung." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Füllsättigung" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Wie gesättigt die Füllflächen des Gruppen-Overlays sind.\n\n100% entspricht der vollen Farbe der Gruppe; niedrigere Werte verblassen in Richtung Grau." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Fülltransparenz verwenden" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Aktiviert Transparenz für die Füllflächen des Gruppen-Overlays.\n\nWenn deaktiviert, zeigt das Overlay eine durchgehend deckende Farbe an, die alle anderen visuellen Elemente verdeckt." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Debug-Protokollierung aktivieren" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Schreibt ausführliche Einträge der Debug-Ebene in die Protokolldatei des Mods.\n\nDies kann die Leistung beeinträchtigen." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Debug-Moddaten protokollieren" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Schreibt den gesamten Mod-Zustand (Gruppen, Diensteinrichtungen usw.) in die Protokolldatei des Mods.\n\nFüge die Protokolldatei bei, wenn du einen Fehlerbericht einreichst." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Fehler melden" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Protokolliert Debug-Daten und öffnet anschließend den GitHub-Issue-Tracker des Mods in deinem Browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Aktualisierungsrate" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "Die Anzahl an Sekunden, die zwischen Aktualisierungen der aggregierten Bezirksinformationen in der Benutzeroberfläche gewartet wird.\n\nHäufiges Aktualisieren kann sich negativ auf die Leistung auswirken." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Alle Einstellungen zurücksetzen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Setzt alle Mod-Einstellungen auf ihre Standardwerte zurück." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Alle Mod-Einstellungen werden auf ihre Standardwerte zurückgesetzt.\r\nMöchtest du fortfahren?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "Die installierte Version des Mods.\n\nGib diese an, wenn du einen Fehlerbericht einreichst." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Release-Kanal" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Mod-Daten entfernen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Löscht alle Bezirksgruppen, Diensteinrichtungszuweisungen und Overlay-Ressourcen, die der Mod dem aktuellen Spielstand hinzugefügt hat." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Alle Bezirksgruppen, Diensteinrichtungszuweisungen und Overlay-Daten werden endgültig aus diesem Spielstand gelöscht.\r\nDies kann nicht rückgängig gemacht werden.\r\nMöchtest du fortfahren?" },

                { LocalizationKey.PanelTitle, "District Groups" },
                { LocalizationKey.NewGroupButton, "Neue Gruppe" },
                { LocalizationKey.NewGroupButtonTooltip, "Fügt eine neue Gruppe ohne Mitgliedsbezirke hinzu." },
                { LocalizationKey.NewGroupDefaultName, "Neue Gruppe {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtert die Liste der Gruppen nach ihrem **Typ**." },
                { LocalizationKey.AllGroupsLabel, "Alle Gruppen" },
                { LocalizationKey.NoGroupsYet, "Noch keine Gruppen vorhanden. Erstelle eine mit der Schaltfläche NEUE GRUPPE." },
                { LocalizationKey.NoGroupsMatchFilter, "Keine Gruppen entsprechen diesem Filter." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Alle Bezirke anzeigen" },
                { LocalizationKey.ShowGroupOverlayLabel, "Gruppen-Overlay anzeigen" },
                { LocalizationKey.ShowServiceBuildingsLabel, "Diensteinrichtungen anzeigen" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Löscht die Gruppe endgültig." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Zugewiesene Gebäude verlieren ihre **versorgten Bezirke**." },
                { LocalizationKey.TypePickerTooltipLine1, "Ändert den **Typ** der Gruppe." },
                { LocalizationKey.TypePickerTooltipLine2, "**Allgemeine** Gruppen können jeder Diensteinrichtung zugewiesen werden." },
                { LocalizationKey.TypePickerTooltipLine3, "Alle anderen Typen stehen nur passenden Diensteinrichtungen zur Verfügung." },
                { LocalizationKey.DeleteGroupConfirmMessage, "\"{NAME}\" ist {COUNT} Diensteinrichtung(en) zugewiesen.\nZugewiesene Diensteinrichtung(en) werden wieder die gesamte Stadt versorgen." },
                { LocalizationKey.DeleteGroupDialogTitle, "Bezirksgruppe löschen?" },
                { LocalizationKey.DeleteGroupConfirm, "Gruppe löschen" },
                { LocalizationKey.DeleteGroupCancel, "Gruppe behalten" },
                { LocalizationKey.RemoveMemberTooltip, "Entfernt den Bezirk aus der Gruppe." },
                { LocalizationKey.SelectDistrictsButton, "Bezirke auswählen" },
                { LocalizationKey.GroupColorTooltip, "Gruppenfarbe" },
                { LocalizationKey.NameInputTooltip, "Auswählen, um den Namen bearbeiten zu können." },
                { LocalizationKey.MetadataDistrictsTooltip, "Bezirke" },
                { LocalizationKey.MetadataBuildingsTooltip, "Zugewiesene Gebäude" },
                { LocalizationKey.MetadataPopulationTooltip, "Bevölkerung" },

                { LocalizationKey.ToggleTooltipTitle, "**BEZIRKSGRUPPEN**" },
                { LocalizationKey.ToggleTooltipBody, "Erstelle Gruppen von Bezirken, die du Diensteinrichtungen zuweisen kannst, um deren **versorgte Bezirke** automatisch zu verwalten." },

                { LocalizationKey.SectionTooltipLine1, "Diensteinrichtungen können einer **Bezirksgruppe** zugewiesen werden." },
                { LocalizationKey.SectionTooltipLine2, "Bei Zuweisung verwaltet die Gruppe die **versorgten Bezirke** für das Gebäude." },
                { LocalizationKey.SectionTooltipLine3, "Ohne Zuweisung werden die **versorgten Bezirke** manuell verwaltet." },
                { LocalizationKey.SectionTooltipLine4, "HINWEIS: Es kann einige Sekunden dauern, bis sich die Anzeige des Infopanels nach einer Änderung der Zuweisung aktualisiert." },
                { LocalizationKey.SectionLabel, "BEZIRKSGRUPPE" },
                { LocalizationKey.OperatingDistrictsLabel, "Versorgte Bezirke" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Dieses Gebäude ist derzeit einer Bezirksgruppe zugewiesen." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**Stadtbezirk**-Zuweisungen werden von der **zugewiesenen Bezirksgruppe** verwaltet." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Wenn die **Bezirksgruppe** keinen **Stadtbezirk** enthält, bietet dieses Gebäude seine Dienstleistung überall innerhalb seines **Einsatzgebiets** an." },
                { LocalizationKey.UnassignOption, "Zuweisung aufheben" },
                { LocalizationKey.UnassignTooltipDisabled, "Keine Gruppe zugewiesen." },
                { LocalizationKey.UnassignTooltipEnabled, "Entfernt die aktuelle Gruppenzuweisung." },
                { LocalizationKey.UnassignedLabel, "Nicht zugewiesen" },
                { LocalizationKey.GroupSearchTitle, "Bezirksgruppe auswählen" },
                { LocalizationKey.SearchGroupsPlaceholder, "Suchen..." },
                { LocalizationKey.NoGroupsMatchSearch, "Keine Gruppen entsprechen deiner Suche." },
                { LocalizationKey.NoGroupsInSection, "Keine Gruppen gefunden." },

                { LocalizationKey.TypeGeneric, "Allgemein" },
                { LocalizationKey.TypePolice, "Polizei" },
                { LocalizationKey.TypeFire, "Feuerwehr" },
                { LocalizationKey.TypeHealthcare, "Gesundheitsfürsorge" },
                { LocalizationKey.TypeDeathcare, "Bestattung" },
                { LocalizationKey.TypeGarbage, "Müllverwaltung" },
                { LocalizationKey.TypeEducationElementary, "Grundschule" },
                { LocalizationKey.TypeEducationHighSchool, "Sekundarschule" },
                { LocalizationKey.TypeEducationCollege, "College" },
                { LocalizationKey.TypeEducationUniversity, "Universität" },
                { LocalizationKey.TypePost, "Post" },
                { LocalizationKey.TypeParks, "Parks" },
                { LocalizationKey.TypeWelfare, "Sozialämter" },
            };
        }

        public void Unload() { }
    }
}

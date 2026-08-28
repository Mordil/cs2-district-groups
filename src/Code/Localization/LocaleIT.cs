using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleIT : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleIT(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "Gruppi di Quartieri" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Principale" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Overlay" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Sviluppatore" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Predefinito" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "UI Overlay" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Risoluzione problemi" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Larghezza del bordo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Larghezza delle linee di confine colorate del gruppo di quartieri disegnate sulla mappa." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacità del bordo" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacità delle linee di confine colorate del gruppo di quartieri disegnate sulla mappa.\n\n0% è completamente trasparente, 100% è completamente opaco." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "Scostamento in altezza" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "Uno scostamento rispetto all'altezza del terreno a cui viene disegnato l'overlay del gruppo di quartieri." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Desaturazione della scena" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Quanto viene desaturato il resto della scena mentre l'overlay del gruppo è visibile.\n\n0% lascia la scena inalterata, 100% è in scala di grigi." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Saturazione del riempimento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Quanto sono sature le aree di riempimento colorate del gruppo di quartieri.\n\n100% è il colore pieno del gruppo; valori più bassi sfumano verso il grigio." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Attiva log di debug" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Scrive voci dettagliate di livello Debug nel file di log della mod.\n\nQuesto potrebbe influire sulle prestazioni." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Registra dati di debug della mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Scrive tutto lo stato della mod (gruppi, edifici di servizio, ecc.) nel file di log della mod.\n\nAllega il file di log quando segnali un bug." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Segnala un bug" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Registra i dati di debug, poi apre il tracker delle segnalazioni GitHub della mod nel tuo browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Frequenza di aggiornamento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "Il numero di secondi da attendere tra gli aggiornamenti dell'interfaccia per le informazioni aggregate sui quartieri.\n\nAggiornare frequentemente potrebbe influire negativamente sulle prestazioni." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Ripristina tutte le impostazioni" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Ripristina tutte le impostazioni della mod ai valori predefiniti." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Tutte le impostazioni della mod torneranno ai valori predefiniti.\r\nVuoi procedere?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Versione" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "La versione installata della mod.\n\nIncludi questa informazione quando segnali un bug." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Canale di rilascio" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Rimuovi dati della mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Elimina tutti i gruppi di quartieri, le assegnazioni degli edifici di servizio e le risorse di sovrapposizione che la mod ha aggiunto alla partita corrente." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Tutti i gruppi di quartieri, le assegnazioni degli edifici di servizio e i dati di sovrapposizione verranno eliminati definitivamente da questa partita.\r\nQuesta azione non può essere annullata.\r\nVuoi procedere?" },

                { LocalizationKey.PanelTitle, "Gruppi di Quartieri" },
                { LocalizationKey.NewGroupButton, "Nuovo Gruppo" },
                { LocalizationKey.NewGroupButtonTooltip, "Aggiunge un nuovo gruppo senza quartieri membri." },
                { LocalizationKey.NewGroupDefaultName, "Nuovo Gruppo {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtra l'elenco dei gruppi per **tipo**." },
                { LocalizationKey.AllGroupsLabel, "Tutti i Gruppi" },
                { LocalizationKey.NoGroupsYet, "Ancora nessun gruppo. Creane uno con il pulsante NUOVO GRUPPO." },
                { LocalizationKey.NoGroupsMatchFilter, "Nessun gruppo corrisponde a questo filtro." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Mostra tutti i quartieri" },
                { LocalizationKey.ShowGroupOverlayLabel, "Mostra overlay del gruppo" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Elimina definitivamente il gruppo." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Gli edifici assegnati perderanno i loro **quartieri operativi**." },
                { LocalizationKey.TypePickerTooltipLine1, "Cambia il **tipo** del gruppo." },
                { LocalizationKey.TypePickerTooltipLine2, "I gruppi **generici** possono essere assegnati a qualsiasi edificio di servizio." },
                { LocalizationKey.TypePickerTooltipLine3, "Tutti gli altri tipi sono disponibili solo per gli edifici di servizio corrispondenti." },
                { LocalizationKey.DeleteGroupConfirmMessage, "\"{NAME}\" è assegnato a {COUNT} edificio/i di servizio.\nGli edifici di servizio assegnati torneranno a servire l'intera città." },
                { LocalizationKey.DeleteGroupDialogTitle, "Eliminare il Gruppo di Quartieri?" },
                { LocalizationKey.DeleteGroupConfirm, "Elimina gruppo" },
                { LocalizationKey.DeleteGroupCancel, "Mantieni gruppo" },
                { LocalizationKey.RemoveMemberTooltip, "Rimuovi il quartiere dal gruppo." },
                { LocalizationKey.SelectDistrictsButton, "Seleziona Quartieri" },
                { LocalizationKey.GroupColorTooltip, "Colore del Gruppo" },
                { LocalizationKey.NameInputTooltip, "Seleziona per modificare il nome." },
                { LocalizationKey.MetadataDistrictsTooltip, "Quartieri" },
                { LocalizationKey.MetadataBuildingsTooltip, "Edifici assegnati" },
                { LocalizationKey.MetadataPopulationTooltip, "Popolazione" },

                { LocalizationKey.ToggleTooltipTitle, "**GRUPPI DI QUARTIERI**" },
                { LocalizationKey.ToggleTooltipBody, "Crea gruppi di quartieri da assegnare agli edifici di servizio per la gestione automatica dei **quartieri operativi**." },

                { LocalizationKey.SectionTooltipLine1, "Gli edifici di servizio possono essere assegnati a un **gruppo di quartieri**." },
                { LocalizationKey.SectionTooltipLine2, "Quando assegnato, il gruppo gestirà i **quartieri operativi** dell'edificio." },
                { LocalizationKey.SectionTooltipLine3, "Quando non assegnato, i **quartieri operativi** vengono gestiti manualmente." },
                { LocalizationKey.SectionTooltipLine4, "NOTA: il pannello informativo potrebbe richiedere alcuni secondi per aggiornarsi visivamente dopo la modifica dell'assegnazione." },
                { LocalizationKey.SectionLabel, "GRUPPO DI QUARTIERI" },
                { LocalizationKey.OperatingDistrictsLabel, "Quartieri operativi" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Questo edificio è attualmente assegnato a un gruppo di quartieri." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "Le assegnazioni di **quartiere** saranno gestite dal **gruppo di quartieri assegnato**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Se il **gruppo di quartieri** non contiene alcun **quartiere**, questo edificio fornirà i suoi servizi ovunque all'interno del suo **raggio operativo**." },
                { LocalizationKey.UnassignOption, "Rimuovi assegnazione" },
                { LocalizationKey.UnassignTooltipDisabled, "Nessun gruppo assegnato." },
                { LocalizationKey.UnassignTooltipEnabled, "Rimuove l'assegnazione del gruppo corrente." },
                { LocalizationKey.UnassignedLabel, "Non assegnato" },
                { LocalizationKey.GroupSearchTitle, "Seleziona Gruppo di Quartieri" },
                { LocalizationKey.SearchGroupsPlaceholder, "Cerca..." },
                { LocalizationKey.NoGroupsMatchSearch, "Nessun gruppo corrisponde alla tua ricerca." },
                { LocalizationKey.NoGroupsInSection, "Nessun gruppo trovato." },

                { LocalizationKey.TypeGeneric, "Generico" },
                { LocalizationKey.TypePolice, "Polizia" },
                { LocalizationKey.TypeFire, "Antincendio" },
                { LocalizationKey.TypeHealthcare, "Assistenza sanitaria" },
                { LocalizationKey.TypeDeathcare, "Pompe funebri" },
                { LocalizationKey.TypeGarbage, "Gestione dei rifiuti" },
                { LocalizationKey.TypeEducationElementary, "Scuola elementare" },
                { LocalizationKey.TypeEducationHighSchool, "Scuola media e superiore" },
                { LocalizationKey.TypeEducationCollege, "College" },
                { LocalizationKey.TypeEducationUniversity, "Università" },
                { LocalizationKey.TypePost, "Posta" },
                { LocalizationKey.TypeParks, "Parchi" },
                { LocalizationKey.TypeWelfare, "Assistenza sociale" },
            };
        }

        public void Unload() { }
    }
}

using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocalePL : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocalePL(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "Grupy Dzielnic" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Główne" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Nakładka" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Deweloperskie" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Domyślne" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Rozwiązywanie problemów" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Szerokość obramowania" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Szerokość linii obramowania dzielnic rysowanych na nakładce." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "Przezroczystość obramowania" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "Przezroczystość linii obramowania dzielnic rysowanych na nakładce.\n\n0% oznacza pełną nieprzezroczystość, 100% pełną przezroczystość." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Desaturacja scenerii" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Jak silnie desaturowana jest pozostała część scenerii, gdy widoczna jest nakładka grupy.\n\n0% oznacza brak zmian, 100% to odcienie szarości." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Nasycenie wypełnienia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Jak nasycone są obszary wypełnienia nakładki grupy.\n\n100% to pełny kolor grupy; niższe wartości przechodzą w stronę szarości." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Użyj przezroczystości wypełnienia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Włącza przezroczystość obszarów wypełnienia nakładki grupy.\n\nGdy jest wyłączona, nakładka wyświetla jednolity, w pełni nieprzezroczysty kolor, który zasłania wszystkie inne elementy wizualne." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "Pokaż etykiety grup" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "Gdy jest włączona, nazwy grup będą wyświetlane wraz z nakładką na mapie.\n\nMoże to wpłynąć na wydajność." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Włącz logi debugowania" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Zapisuje szczegółowe wpisy poziomu debugowania do pliku dziennika moda.\n\nMoże to wpłynąć na wydajność." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Zapisz dane debugowania moda" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Zapisuje cały stan moda (grupy, budynki usługowe itd.) do pliku dziennika moda.\n\nDołącz plik dziennika podczas zgłaszania błędu." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Zgłoś błąd" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Zapisuje dane debugowania, a następnie otwiera w przeglądarce stronę zgłoszeń błędów moda na GitHubie." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Częstotliwość odświeżania" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "Liczba sekund oczekiwania między aktualizacjami interfejsu dla zagregowanych informacji o dzielnicach.\n\nCzęste aktualizowanie może negatywnie wpłynąć na wydajność." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Zresetuj wszystkie ustawienia" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Przywraca wszystkie ustawienia moda do wartości domyślnych." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Wszystkie ustawienia moda zostaną przywrócone do wartości domyślnych.\r\nCzy chcesz kontynuować?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Wersja" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "Zainstalowana wersja moda.\n\nDołącz tę informację podczas zgłaszania błędu." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Kanał wydania" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Usuń dane moda" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Usuwa wszystkie grupy dzielnic, przypisania budynków usługowych oraz zasoby nakładki, które mod dodał do bieżącego zapisu gry." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Wszystkie grupy dzielnic, przypisania budynków usługowych i dane nakładki zostaną bezpowrotnie usunięte z tego zapisu gry.\r\nTej operacji nie można odwrócić.\r\nCzy chcesz kontynuować?" },

                { LocalizationKey.PanelTitle, "Grupy Dzielnic" },
                { LocalizationKey.GroupsTabLabel, "Grupy" },
                { LocalizationKey.AssignmentsTabLabel, "Budynki usługowe" },
                { LocalizationKey.SelectTypeForAssignments, "Wybierz typ usługi, aby wyświetlić jej budynki usługowe." },
                { LocalizationKey.NoServiceBuildingsMatchFilter, "Brak budynków usługowych zgodnych z tym filtrem." },
                { LocalizationKey.HideAssignedBuildingsLabel, "Ukryj przypisane budynki" },
                { LocalizationKey.NewGroupButton, "Nowa grupa" },
                { LocalizationKey.NewGroupButtonTooltip, "Dodaje nową grupę bez przypisanych dzielnic." },
                { LocalizationKey.NewGroupDefaultName, "Nowa grupa {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtruj listę grup według ich **typu**." },
                { LocalizationKey.AllGroupsLabel, "Wszystkie grupy" },
                { LocalizationKey.NoGroupsYet, "Brak grup. Utwórz jedną za pomocą przycisku NOWA GRUPA." },
                { LocalizationKey.NoGroupsMatchFilter, "Brak grup zgodnych z tym filtrem." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Pokaż wszystkie dzielnice" },
                { LocalizationKey.ShowGroupOverlayLabel, "Pokaż nakładkę grupy" },
                { LocalizationKey.ShowServiceBuildingsLabel, "Pokaż budynki usługowe" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Trwale usuwa grupę." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Przypisane budynki utracą swoje **obsługiwane dzielnice**." },
                { LocalizationKey.TypePickerTooltipLine1, "Zmienia **typ** grupy." },
                { LocalizationKey.TypePickerTooltipLine2, "Grupy typu **uniwersalny** można przypisać do każdego budynku usługowego." },
                { LocalizationKey.TypePickerTooltipLine3, "Wszystkie inne typy są dostępne tylko dla odpowiadających im budynków usługowych." },
                { LocalizationKey.DeleteGroupConfirmMessage, "„{NAME}” jest przypisana do {COUNT} budynków usługowych.\nPrzypisane budynki usługowe zaczną ponownie obsługiwać całe miasto." },
                { LocalizationKey.DeleteGroupDialogTitle, "Usunąć grupę dzielnic?" },
                { LocalizationKey.DeleteGroupConfirm, "Usuń grupę" },
                { LocalizationKey.DeleteGroupCancel, "Zachowaj grupę" },
                { LocalizationKey.RemoveMemberTooltip, "Usuwa dzielnicę z grupy." },
                { LocalizationKey.SelectDistrictsButton, "Wybierz dzielnice" },
                { LocalizationKey.GroupColorTooltip, "Kolor grupy" },
                { LocalizationKey.NameInputTooltip, "Wybierz, aby edytować nazwę." },
                { LocalizationKey.MetadataDistrictsTooltip, "Dzielnice" },
                { LocalizationKey.MetadataBuildingsTooltip, "Przypisane budynki" },
                { LocalizationKey.MetadataPopulationTooltip, "Populacja" },

                { LocalizationKey.ToggleTooltipTitle, "**GRUPY DZIELNIC**" },
                { LocalizationKey.ToggleTooltipBody, "Twórz grupy dzielnic, które można przypisywać do budynków usługowych, aby automatycznie zarządzać ich **obsługiwanymi dzielnicami**." },

                { LocalizationKey.SectionTooltipLine1, "Budynki usługowe można przypisać do **grupy dzielnic**." },
                { LocalizationKey.SectionTooltipLine2, "Po przypisaniu grupa zarządza **obsługiwanymi dzielnicami** budynku." },
                { LocalizationKey.SectionTooltipLine3, "Bez przypisania **obsługiwane dzielnice** są zarządzane ręcznie." },
                { LocalizationKey.SectionTooltipLine4, "UWAGA: Panel informacyjny może potrzebować kilku sekund, aby zaktualizować widok po zmianie przypisania." },
                { LocalizationKey.SectionLabel, "GRUPA DZIELNIC" },
                { LocalizationKey.OperatingDistrictsLabel, "Obsługiwane dzielnice" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Ten budynek jest obecnie przypisany do grupy dzielnic." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "Przypisania **dzielnicy** są zarządzane przez **przypisaną grupę dzielnic**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Jeśli **grupa dzielnic** nie zawiera żadnej **dzielnicy**, budynek będzie świadczyć usługi wszędzie w swoim **zasięgu operacyjnym**." },
                { LocalizationKey.UnassignOption, "Usuń przypisanie" },
                { LocalizationKey.UnassignTooltipDisabled, "Brak przypisanej grupy." },
                { LocalizationKey.UnassignTooltipEnabled, "Usuwa bieżące przypisanie grupy." },
                { LocalizationKey.UnassignedLabel, "Nieprzypisane" },
                { LocalizationKey.GroupSearchTitle, "Wybierz grupę dzielnic" },
                { LocalizationKey.SearchGroupsPlaceholder, "Szukaj..." },
                { LocalizationKey.NoGroupsMatchSearch, "Brak grup zgodnych z wyszukiwaniem." },
                { LocalizationKey.NoGroupsInSection, "Nie znaleziono grup." },

                { LocalizationKey.TypeGeneric, "Uniwersalny" },
                { LocalizationKey.TypePolice, "Policja" },
                { LocalizationKey.TypeFire, "Straż pożarna" },
                { LocalizationKey.TypeHealthcare, "Służba zdrowia" },
                { LocalizationKey.TypeDeathcare, "Służby pogrzebowe" },
                { LocalizationKey.TypeGarbage, "Gospodarka odpadami" },
                { LocalizationKey.TypeEducationElementary, "Szkoła podstawowa" },
                { LocalizationKey.TypeEducationHighSchool, "Liceum" },
                { LocalizationKey.TypeEducationCollege, "Szkoła pomaturalna" },
                { LocalizationKey.TypeEducationUniversity, "Uniwersytet" },
                { LocalizationKey.TypePost, "Poczta" },
                { LocalizationKey.TypeParks, "Parki" },
                { LocalizationKey.TypeWelfare, "Opieka społeczna" },
            };
        }

        public void Unload() { }
    }
}

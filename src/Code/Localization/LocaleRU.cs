using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleRU : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleRU(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "Группы районов" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Основные" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Слой" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Разработчик" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Общие" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Устранение неполадок" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Толщина границы" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Толщина линий границ районов, отображаемых на слое." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "Прозрачность границы" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "Прозрачность линий границ районов, отображаемых на слое.\n\n0% — полностью непрозрачно, 100% — полностью прозрачно." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Обесцвечивание сцены" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Насколько обесцвечивается остальная часть сцены, когда отображается слой группы.\n\n0% — сцена остаётся без изменений, 100% — полностью чёрно-белая." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Насыщенность заливки" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Насколько насыщена заливка областей слоя группы.\n\n100% — исходный цвет группы, при меньших значениях цвет становится более серым." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Использовать прозрачность заливки" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Включает прозрачность для областей заливки слоя группы.\n\nЕсли отключено, слой будет отображаться сплошным, полностью непрозрачным цветом, скрывающим все остальные визуальные элементы." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Включить отладочное логирование" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Записывает подробные отладочные записи в файл журнала мода.\n\nЭто может повлиять на производительность." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Записать отладочные данные мода" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Записывает всё состояние мода (группы, здания служб и т.д.) в файл журнала мода.\n\nПриложите файл журнала при подаче отчёта об ошибке." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Сообщить об ошибке" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Записывает отладочные данные, затем открывает трекер задач мода на GitHub в вашем браузере." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Частота обновления" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "Количество секунд ожидания между обновлениями интерфейса для агрегированной информации о районах.\n\nЧастое обновление может негативно повлиять на производительность." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Сбросить все настройки" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Сбрасывает все настройки мода к значениям по умолчанию." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Все настройки мода будут возвращены к значениям по умолчанию.\r\nВы хотите продолжить?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Версия" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "Установленная версия мода.\n\nУкажите её при подаче отчёта об ошибке." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Канал выпуска" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Удалить данные мода" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Удаляет все группы районов, привязки зданий служб и ресурсы слоя, добавленные модом в текущее сохранение." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Все группы районов, привязки зданий служб и данные слоя будут безвозвратно удалены из этого сохранения.\r\nЭто действие невозможно отменить.\r\nВы хотите продолжить?" },

                { LocalizationKey.PanelTitle, "Группы районов" },
                { LocalizationKey.NewGroupButton, "Новая группа" },
                { LocalizationKey.NewGroupButtonTooltip, "Добавляет новую группу без районов-участников." },
                { LocalizationKey.NewGroupDefaultName, "Новая группа {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Фильтровать список групп по их **типу**." },
                { LocalizationKey.AllGroupsLabel, "Все группы" },
                { LocalizationKey.NoGroupsYet, "Групп пока нет. Создайте новую с помощью кнопки НОВАЯ ГРУППА." },
                { LocalizationKey.NoGroupsMatchFilter, "Нет групп, соответствующих этому фильтру." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Показать все районы" },
                { LocalizationKey.ShowGroupOverlayLabel, "Показать слой группы" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Полностью удалить группу." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Назначенные здания потеряют свои **районы обслуживания**." },
                { LocalizationKey.TypePickerTooltipLine1, "Изменить **тип** группы." },
                { LocalizationKey.TypePickerTooltipLine2, "Группы типа **Общий** можно назначить любому зданию службы." },
                { LocalizationKey.TypePickerTooltipLine3, "Все остальные типы доступны только для соответствующих зданий служб." },
                { LocalizationKey.DeleteGroupConfirmMessage, "«{NAME}» назначена {COUNT} зданиям служб.\nНазначенные здания служб будут снова обслуживать весь город." },
                { LocalizationKey.DeleteGroupDialogTitle, "Удалить группу районов?" },
                { LocalizationKey.DeleteGroupConfirm, "Удалить группу" },
                { LocalizationKey.DeleteGroupCancel, "Оставить группу" },
                { LocalizationKey.RemoveMemberTooltip, "Удалить район из группы." },
                { LocalizationKey.SelectDistrictsButton, "Выбрать районы" },
                { LocalizationKey.GroupColorTooltip, "Цвет группы" },
                { LocalizationKey.NameInputTooltip, "Выберите, чтобы изменить название." },
                { LocalizationKey.MetadataDistrictsTooltip, "Районы" },
                { LocalizationKey.MetadataBuildingsTooltip, "Назначенные здания" },
                { LocalizationKey.MetadataPopulationTooltip, "Население" },

                { LocalizationKey.ToggleTooltipTitle, "**ГРУППЫ РАЙОНОВ**" },
                { LocalizationKey.ToggleTooltipBody, "Создавайте группы районов и назначайте их зданиям служб для автоматического управления **районами обслуживания**." },

                { LocalizationKey.SectionTooltipLine1, "Зданиям служб можно назначить **группу районов**." },
                { LocalizationKey.SectionTooltipLine2, "После назначения группа будет управлять **районами обслуживания** этого здания." },
                { LocalizationKey.SectionTooltipLine3, "Без назначенной группы **районы обслуживания** управляются вручную." },
                { LocalizationKey.SectionTooltipLine4, "ПРИМЕЧАНИЕ: панели информации может потребоваться несколько секунд, чтобы визуально обновиться после изменения назначения." },
                { LocalizationKey.SectionLabel, "ГРУППА РАЙОНОВ" },
                { LocalizationKey.OperatingDistrictsLabel, "Районы обслуживания" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Это здание сейчас назначено группе районов." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "Назначением **района города** будет управлять **назначенная группа районов**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Если **группа районов** не содержит ни одного **района города**, это здание будет предоставлять свои услуги везде в пределах своего **радиуса действия**." },
                { LocalizationKey.UnassignOption, "Отменить назначение" },
                { LocalizationKey.UnassignTooltipDisabled, "Группа не назначена." },
                { LocalizationKey.UnassignTooltipEnabled, "Удаляет текущее назначение группы." },
                { LocalizationKey.UnassignedLabel, "Не назначено" },
                { LocalizationKey.GroupSearchTitle, "Выбор группы районов" },
                { LocalizationKey.SearchGroupsPlaceholder, "Поиск..." },
                { LocalizationKey.NoGroupsMatchSearch, "Нет групп, соответствующих поиску." },
                { LocalizationKey.NoGroupsInSection, "Группы не найдены." },

                { LocalizationKey.TypeGeneric, "Общий" },
                { LocalizationKey.TypePolice, "Полиция" },
                { LocalizationKey.TypeFire, "Пожар" },
                { LocalizationKey.TypeHealthcare, "Здравоохранение" },
                { LocalizationKey.TypeDeathcare, "Ритуальные услуги" },
                { LocalizationKey.TypeGarbage, "Отходы" },
                { LocalizationKey.TypeEducationElementary, "Начальная школа" },
                { LocalizationKey.TypeEducationHighSchool, "Средняя школа" },
                { LocalizationKey.TypeEducationCollege, "Колледж" },
                { LocalizationKey.TypeEducationUniversity, "Университет" },
                { LocalizationKey.TypePost, "Почта" },
                { LocalizationKey.TypeParks, "Парковая служба" },
                { LocalizationKey.TypeWelfare, "Социальное обеспечение" },
            };
        }

        public void Unload() { }
    }
}

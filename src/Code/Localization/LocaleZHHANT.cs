using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleZHHANT : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleZHHANT(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "行政區群組" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "主要" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "疊加圖層" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "開發者" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "預設" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "疊加圖層介面" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "疑難排解" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "邊框寬度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "地圖上繪製的行政區群組邊界線的寬度。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "邊框不透明度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "地圖上繪製的行政區群組邊界線的不透明度。\n\n0% 為完全透明，100% 為完全不透明。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "高度偏移" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "行政區群組疊加圖層繪製高度相對於地形高度的偏移量。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "場景去飽和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "顯示群組疊加圖層時，場景其他部分的去飽和程度。\n\n0% 表示場景不受影響，100% 表示變為灰階。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "填色飽和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "行政區群組填色區域的飽和程度。\n\n100% 為群組的完整顏色，數值越低則越趨近灰色。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "啟用偵錯記錄" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "將詳細的偵錯層級項目寫入模組的記錄檔。\n\n這可能會影響效能。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "記錄模組偵錯資料" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "將所有模組狀態（群組、服務建築等）寫入模組的記錄檔。\n\n提交錯誤報告時請附上記錄檔。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "提交錯誤報告" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "記錄偵錯資料，然後在您的瀏覽器中開啟模組的 GitHub 問題追蹤頁面。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "重設所有設定" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "將所有模組設定重設為預設值。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "所有模組設定將還原為預設值。\r\n是否要繼續？" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "版本" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "目前安裝的模組版本。\n\n提交錯誤報告時請附上此資訊。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "移除模組資料" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "刪除模組在目前存檔中新增的所有行政區群組、服務建築指派及疊加層資源。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "此存檔中的所有行政區群組、服務建築指派及疊加層資料將被永久刪除。\r\n此操作無法復原。\r\n是否要繼續？" },

                { LocalizationKey.PanelTitle, "行政區群組" },
                { LocalizationKey.NewGroupButton, "新增群組" },
                { LocalizationKey.NewGroupButtonTooltip, "新增一個沒有任何成員行政區的群組。" },
                { LocalizationKey.NewGroupDefaultName, "新群組 {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "依**類型**篩選群組清單。" },
                { LocalizationKey.AllGroupsLabel, "所有群組" },
                { LocalizationKey.NoGroupsYet, "尚無群組。請使用「新增群組」按鈕建立一個。" },
                { LocalizationKey.NoGroupsMatchFilter, "沒有符合此篩選條件的群組。" },
                { LocalizationKey.DisplayDistrictAreasLabel, "顯示所有行政區" },
                { LocalizationKey.ShowGroupOverlayLabel, "顯示群組疊加圖層" },

                { LocalizationKey.DeleteGroupTooltipLine1, "永久刪除此群組。" },
                { LocalizationKey.DeleteGroupTooltipLine2, "已指派的建築將失去其**行政區**。" },
                { LocalizationKey.TypePickerTooltipLine1, "變更群組的**類型**。" },
                { LocalizationKey.TypePickerTooltipLine2, "**通用**群組可指派給任何服務建築。" },
                { LocalizationKey.TypePickerTooltipLine3, "其他所有類型僅適用於對應的服務建築。" },
                { LocalizationKey.DeleteGroupConfirmMessage, "「{NAME}」已指派給 {COUNT} 個服務建築。\n已指派的服務建築將重新服務整座城市。" },
                { LocalizationKey.DeleteGroupDialogTitle, "刪除行政區群組？" },
                { LocalizationKey.DeleteGroupConfirm, "刪除群組" },
                { LocalizationKey.DeleteGroupCancel, "保留群組" },
                { LocalizationKey.RemoveMemberTooltip, "將該行政區從群組中移除。" },
                { LocalizationKey.SelectDistrictsButton, "選擇行政區" },
                { LocalizationKey.GroupColorTooltip, "群組顏色" },
                { LocalizationKey.NameInputTooltip, "選取以修改名稱。" },

                { LocalizationKey.ToggleTooltipTitle, "**行政區群組**" },
                { LocalizationKey.ToggleTooltipBody, "建立行政區群組並指派給服務建築，自動管理其**行政區**。" },

                { LocalizationKey.SectionTooltipLine1, "服務建築可以被指派給**行政區群組**。" },
                { LocalizationKey.SectionTooltipLine2, "指派後，群組將為該建築管理**行政區**。" },
                { LocalizationKey.SectionTooltipLine3, "取消指派後，**行政區**將改為手動管理。" },
                { LocalizationKey.SectionTooltipLine4, "注意：變更指派後，資訊面板可能需要幾秒鐘才會更新顯示。" },
                { LocalizationKey.SectionLabel, "行政區群組" },
                { LocalizationKey.OperatingDistrictsLabel, "操作區" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "此建築目前已指派給一個行政區群組。" },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**城市行政區**的指派將由**已指派的行政區群組**管理。" },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "如果**行政區群組**沒有任何**城市行政區**，此建築將在其**運作範圍**內的任何地方提供服務。" },
                { LocalizationKey.UnassignOption, "取消指派" },
                { LocalizationKey.UnassignTooltipDisabled, "尚未指派群組。" },
                { LocalizationKey.UnassignTooltipEnabled, "移除目前的群組指派。" },
                { LocalizationKey.UnassignedLabel, "未指派" },
                { LocalizationKey.GroupSearchTitle, "選擇行政區群組" },
                { LocalizationKey.SearchGroupsPlaceholder, "搜尋…" },
                { LocalizationKey.NoGroupsMatchSearch, "沒有符合搜尋條件的群組。" },
                { LocalizationKey.NoGroupsInSection, "找不到群組。" },

                { LocalizationKey.TypeGeneric, "通用" },
                { LocalizationKey.TypePolice, "警察" },
                { LocalizationKey.TypeFire, "火災" },
                { LocalizationKey.TypeHealthcare, "醫療衛生" },
                { LocalizationKey.TypeDeathcare, "殯葬服務" },
                { LocalizationKey.TypeGarbage, "垃圾" },
                { LocalizationKey.TypeEducationElementary, "小學" },
                { LocalizationKey.TypeEducationHighSchool, "中學" },
                { LocalizationKey.TypeEducationCollege, "大專" },
                { LocalizationKey.TypeEducationUniversity, "大學" },
                { LocalizationKey.TypePost, "郵政" },
                { LocalizationKey.TypeParks, "公園" },
                { LocalizationKey.TypeWelfare, "福利" },
            };
        }

        public void Unload() { }
    }
}

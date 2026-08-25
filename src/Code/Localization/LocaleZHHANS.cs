using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleZHHANS : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleZHHANS(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "区域组" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "主要" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "叠加层" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "开发者" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "默认" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "叠加层界面" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "故障排除" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "边框宽度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "地图上绘制的彩色区域组边界线的宽度。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "边框透明度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "地图上绘制的彩色区域组边界线的透明度。\n\n0% 为完全透明，100% 为完全不透明。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "高度偏移" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "区域组叠加层绘制高度相对于地形高度的偏移量。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "场景去饱和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "显示区域组叠加层时，场景其余部分的去饱和程度。\n\n0% 保持场景不变，100% 为完全灰度。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "填充饱和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "彩色区域组填充区域的饱和程度。\n\n100% 为该区域组的完整颜色；数值越低，颜色越趋向灰色。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "启用调试日志" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "将详细的调试级别记录写入模组的日志文件。\n\n这可能会影响性能。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "记录调试模组数据" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "将所有模组状态（区域组、服务建筑等）写入模组的日志文件。\n\n提交错误报告时请附上该日志文件。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "提交错误报告" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "记录调试数据，然后在浏览器中打开模组的 GitHub 问题跟踪页面。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "重置所有设置" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "将所有模组设置恢复为默认值。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "所有模组设置都将恢复为默认值。\r\n是否继续？" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "版本" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "已安装的模组版本。\n\n提交错误报告时请附上此信息。" },

                { LocalizationKey.PanelTitle, "区域组" },
                { LocalizationKey.NewGroupButton, "新建区域组" },
                { LocalizationKey.NewGroupButtonTooltip, "添加一个不包含任何成员区域的新区域组。" },
                { LocalizationKey.NewGroupDefaultName, "新区域组 {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "按**类型**筛选区域组列表。" },
                { LocalizationKey.AllGroupsLabel, "所有区域组" },
                { LocalizationKey.NoGroupsYet, "尚无区域组，点击「新建区域组」按钮创建一个。" },
                { LocalizationKey.NoGroupsMatchFilter, "没有区域组匹配此筛选条件。" },
                { LocalizationKey.DisplayDistrictAreasLabel, "显示所有区域" },
                { LocalizationKey.ShowGroupOverlayLabel, "显示区域组叠加层" },

                { LocalizationKey.DeleteGroupTooltipLine1, "永久删除该区域组。" },
                { LocalizationKey.DeleteGroupTooltipLine2, "已分配的建筑将失去其**运营区域**。" },
                { LocalizationKey.TypePickerTooltipLine1, "更改区域组的**类型**。" },
                { LocalizationKey.TypePickerTooltipLine2, "**通用**类型的区域组可分配给任何服务建筑。" },
                { LocalizationKey.TypePickerTooltipLine3, "其他所有类型仅适用于匹配的服务建筑。" },
                { LocalizationKey.DeleteGroupConfirmMessage, "“{NAME}”已分配给 {COUNT} 个服务建筑。\n已分配的服务建筑将再次为全市提供服务。" },
                { LocalizationKey.DeleteGroupDialogTitle, "删除区域组？" },
                { LocalizationKey.DeleteGroupConfirm, "删除区域组" },
                { LocalizationKey.DeleteGroupCancel, "保留区域组" },
                { LocalizationKey.RemoveMemberTooltip, "将该区域从区域组中移除。" },
                { LocalizationKey.SelectDistrictsButton, "选择区域" },
                { LocalizationKey.GroupColorTooltip, "区域组颜色" },

                { LocalizationKey.ToggleTooltipTitle, "**区域组**" },
                { LocalizationKey.ToggleTooltipBody, "创建区域组并分配给服务建筑，实现**运营区域**的自动管理。" },

                { LocalizationKey.SectionTooltipLine1, "服务建筑可以被分配一个**区域组**。" },
                { LocalizationKey.SectionTooltipLine2, "分配后，该区域组将管理此建筑的**运营区域**。" },
                { LocalizationKey.SectionTooltipLine3, "未分配时，**运营区域**需手动管理。" },
                { LocalizationKey.SectionTooltipLine4, "注意：更改分配后，信息面板可能需要几秒钟才能完成视觉更新。" },
                { LocalizationKey.SectionLabel, "区域组" },
                { LocalizationKey.OperatingDistrictsLabel, "运营区域" },
                { LocalizationKey.UnassignOption, "取消分配" },
                { LocalizationKey.UnassignTooltipDisabled, "未分配任何区域组。" },
                { LocalizationKey.UnassignTooltipEnabled, "移除当前的区域组分配。" },
                { LocalizationKey.UnassignedLabel, "未分配" },
                { LocalizationKey.GroupSearchTitle, "选择区域组" },
                { LocalizationKey.SearchGroupsPlaceholder, "搜索…" },
                { LocalizationKey.NoGroupsMatchSearch, "没有区域组匹配您的搜索。" },
                { LocalizationKey.NoGroupsInSection, "未找到区域组。" },

                { LocalizationKey.TypeGeneric, "通用" },
                { LocalizationKey.TypePolice, "警察" },
                { LocalizationKey.TypeFire, "消防" },
                { LocalizationKey.TypeHealthcare, "医疗卫生" },
                { LocalizationKey.TypeDeathcare, "殡葬" },
                { LocalizationKey.TypeGarbage, "垃圾" },
                { LocalizationKey.TypeEducationElementary, "小学" },
                { LocalizationKey.TypeEducationHighSchool, "中学" },
                { LocalizationKey.TypeEducationCollege, "学院制大学" },
                { LocalizationKey.TypeEducationUniversity, "综合性大学" },
                { LocalizationKey.TypePost, "邮政" },
                { LocalizationKey.TypeParks, "公园" },
                { LocalizationKey.TypeWelfare, "福利" },
            };
        }

        public void Unload() { }
    }
}

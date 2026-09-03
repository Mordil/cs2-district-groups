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
                { m_Setting.GetSettingsLocaleID(), "市辖区组" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "主要" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "覆盖图" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "开发者" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "默认" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "故障排除" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "边框宽度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "覆盖图上绘制的市辖区边界线的宽度。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "边框透明度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "覆盖图上绘制的市辖区边界线的透明度。\n\n0% 为完全不透明，100% 为完全透明。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "场景去饱和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "显示市辖区组覆盖图时，场景其余部分的去饱和程度。\n\n0% 保持场景不变，100% 为完全灰度。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "填充饱和度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "市辖区组覆盖图填充区域的饱和程度。\n\n100% 为该组的完整颜色；数值越低，颜色越趋向灰色。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "使用填充透明化" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "为市辖区组覆盖图的填充区域启用透明化。\n\n禁用时，覆盖图将显示完全不透明的纯色，遮盖所有其他视觉内容。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "显示组名称" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "启用后，组名称将与覆盖图一同显示在地图上。\n\n这可能会影响性能。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "启用调试日志" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "将详细的调试级别记录写入模组的日志文件。\n\n这可能会影响性能。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "记录调试模组数据" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "将所有模组状态（市辖区组、服务建筑等）写入模组的日志文件。\n\n提交错误报告时请附上该日志文件。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "提交错误报告" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "记录调试数据，然后在浏览器中打开模组的 GitHub 问题跟踪页面。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "刷新率" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "在界面更新聚合市辖区信息之间等待的秒数。\n\n频繁更新可能会对性能产生负面影响。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "重置所有设置" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "将所有模组设置恢复为默认值。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "所有模组设置都将恢复为默认值。\r\n是否继续？" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "版本" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "已安装的模组版本。\n\n提交错误报告时请附上此信息。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "发布渠道" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "移除模组数据" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "删除模组在当前存档中添加的所有市辖区组、服务建筑分配和覆盖图资源。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "此存档中的所有市辖区组、服务建筑分配和覆盖图数据都将被永久删除。\r\n此操作无法撤销。\r\n是否继续？" },

                { LocalizationKey.PanelTitle, "市辖区组" },
                { LocalizationKey.GroupsTabLabel, "市辖区组" },
                { LocalizationKey.AssignmentsTabLabel, "服务建筑" },
                { LocalizationKey.SelectTypeForAssignments, "选择一种服务类型以列出相应的服务建筑。" },
                { LocalizationKey.NoServiceBuildingsMatchFilter, "没有服务建筑匹配此筛选条件。" },
                { LocalizationKey.HideAssignedBuildingsLabel, "隐藏已分配的建筑" },
                { LocalizationKey.NewGroupButton, "新建市辖区组" },
                { LocalizationKey.NewGroupButtonTooltip, "添加一个不包含任何成员市辖区的新市辖区组。" },
                { LocalizationKey.NewGroupDefaultName, "新市辖区组 {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "按**类型**筛选市辖区组列表。" },
                { LocalizationKey.AllGroupsLabel, "所有市辖区组" },
                { LocalizationKey.NoGroupsYet, "尚无市辖区组，点击「新建市辖区组」按钮创建一个。" },
                { LocalizationKey.NoGroupsMatchFilter, "没有市辖区组匹配此筛选条件。" },
                { LocalizationKey.DisplayDistrictAreasLabel, "显示所有市辖区" },
                { LocalizationKey.ShowGroupOverlayLabel, "显示市辖区组覆盖图" },
                { LocalizationKey.ShowServiceBuildingsLabel, "显示服务建筑" },

                { LocalizationKey.DeleteGroupTooltipLine1, "永久删除该市辖区组。" },
                { LocalizationKey.DeleteGroupTooltipLine2, "已分配的建筑将失去其**工作区**。" },
                { LocalizationKey.TypePickerTooltipLine1, "更改市辖区组的**类型**。" },
                { LocalizationKey.TypePickerTooltipLine2, "**通用**类型的市辖区组可分配给任何服务建筑。" },
                { LocalizationKey.TypePickerTooltipLine3, "其他所有类型仅适用于匹配的服务建筑。" },
                { LocalizationKey.DeleteGroupConfirmMessage, "“{NAME}”已分配给 {COUNT} 个服务建筑。\n已分配的服务建筑将再次为全市提供服务。" },
                { LocalizationKey.DeleteGroupDialogTitle, "删除市辖区组？" },
                { LocalizationKey.DeleteGroupConfirm, "删除市辖区组" },
                { LocalizationKey.DeleteGroupCancel, "保留市辖区组" },
                { LocalizationKey.RemoveMemberTooltip, "将该市辖区从市辖区组中移除。" },
                { LocalizationKey.SelectDistrictsButton, "选择市辖区" },
                { LocalizationKey.GroupColorTooltip, "市辖区组颜色" },
                { LocalizationKey.NameInputTooltip, "选择以修改名称。" },
                { LocalizationKey.MetadataDistrictsTooltip, "市辖区" },
                { LocalizationKey.MetadataBuildingsTooltip, "已分配建筑" },
                { LocalizationKey.MetadataPopulationTooltip, "人口" },

                { LocalizationKey.ToggleTooltipTitle, "**市辖区组**" },
                { LocalizationKey.ToggleTooltipBody, "创建市辖区组并分配给服务建筑，实现**工作区**的自动管理。" },

                { LocalizationKey.SectionTooltipLine1, "服务建筑可以被分配一个**市辖区组**。" },
                { LocalizationKey.SectionTooltipLine2, "分配后，该市辖区组将管理此建筑的**工作区**。" },
                { LocalizationKey.SectionTooltipLine3, "未分配时，**工作区**需手动管理。" },
                { LocalizationKey.SectionTooltipLine4, "注意：更改分配后，信息面板可能需要几秒钟才能完成视觉更新。" },
                { LocalizationKey.SectionLabel, "市辖区组" },
                { LocalizationKey.OperatingDistrictsLabel, "工作区" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "此建筑目前已分配给一个市辖区组。" },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**市辖区**的分配将由**已分配的市辖区组**管理。" },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "如果**市辖区组**没有任何**市辖区**，此建筑将在其**运作范围**内的任何地方提供服务。" },
                { LocalizationKey.UnassignOption, "取消分配" },
                { LocalizationKey.UnassignTooltipDisabled, "未分配任何市辖区组。" },
                { LocalizationKey.UnassignTooltipEnabled, "移除当前的市辖区组分配。" },
                { LocalizationKey.UnassignedLabel, "未分配" },
                { LocalizationKey.GroupSearchTitle, "选择市辖区组" },
                { LocalizationKey.SearchGroupsPlaceholder, "搜索…" },
                { LocalizationKey.NoGroupsMatchSearch, "没有市辖区组匹配您的搜索。" },
                { LocalizationKey.NoGroupsInSection, "未找到市辖区组。" },

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

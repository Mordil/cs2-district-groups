using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleKO : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleKO(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "지구 그룹" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "일반" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "오버레이" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "개발자" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "기본" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "오버레이 UI" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "문제 해결" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "테두리 너비" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "지도에 표시되는 지구 그룹 경계선의 너비입니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "테두리 불투명도" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "지도에 표시되는 지구 그룹 경계선의 불투명도입니다.\n\n0%면 완전히 투명하고, 100%면 완전히 불투명합니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "높이 오프셋" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "지구 그룹 오버레이가 그려지는 높이를 지형 높이로부터 얼마나 띄울지 설정합니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "장면 채도 감소" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "그룹 오버레이가 표시되는 동안 나머지 장면의 채도를 얼마나 낮출지 설정합니다.\n\n0%면 장면이 그대로 유지되고, 100%면 완전한 흑백이 됩니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "채우기 채도" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "색상이 있는 지구 그룹 채우기 영역의 채도입니다.\n\n100%면 그룹의 원래 색상이 되고, 값이 낮을수록 회색에 가까워집니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "디버그 로깅 사용" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "모드의 로그 파일에 상세한 디버그 수준 항목을 기록합니다.\n\n성능에 영향을 줄 수 있습니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "디버그 모드 데이터 기록" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "모드의 모든 상태(그룹, 서비스 건물 등)를 모드의 로그 파일에 기록합니다.\n\n버그를 신고할 때 로그 파일을 함께 첨부해 주세요." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "버그 신고" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "디버그 데이터를 기록한 후, 브라우저에서 모드의 GitHub 이슈 트래커를 엽니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "새로 고침 빈도" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "집계된 지구 정보에 대한 UI 업데이트 사이에 대기할 시간(초)입니다.\n\n자주 업데이트하면 성능에 부정적인 영향을 미칠 수 있습니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "모든 설정 초기화" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "모든 모드 설정을 기본값으로 되돌립니다." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "모든 모드 설정이 기본값으로 되돌아갑니다.\r\n계속하시겠습니까?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "버전" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "설치된 모드의 버전입니다.\n\n버그를 신고할 때 이 정보를 포함해 주세요." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "릴리스 채널" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "모드 데이터 제거" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "현재 세이브에 모드가 추가한 모든 지구 그룹, 서비스 건물 할당, 오버레이 리소스를 삭제합니다." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "이 세이브에서 모든 지구 그룹, 서비스 건물 할당, 오버레이 데이터가 영구적으로 삭제됩니다.\r\n이 작업은 되돌릴 수 없습니다.\r\n계속하시겠습니까?" },

                { LocalizationKey.PanelTitle, "지구 그룹" },
                { LocalizationKey.NewGroupButton, "새 그룹" },
                { LocalizationKey.NewGroupButtonTooltip, "소속 지구이 없는 새 그룹을 추가합니다." },
                { LocalizationKey.NewGroupDefaultName, "새 그룹 {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "**유형**별로 그룹 목록을 필터링합니다." },
                { LocalizationKey.AllGroupsLabel, "모든 그룹" },
                { LocalizationKey.NoGroupsYet, "아직 그룹이 없습니다. 새 그룹 버튼으로 그룹을 만들어 보세요." },
                { LocalizationKey.NoGroupsMatchFilter, "이 필터에 일치하는 그룹이 없습니다." },
                { LocalizationKey.DisplayDistrictAreasLabel, "모든 지구 표시" },
                { LocalizationKey.ShowGroupOverlayLabel, "그룹 오버레이 표시" },

                { LocalizationKey.DeleteGroupTooltipLine1, "그룹을 영구적으로 삭제합니다." },
                { LocalizationKey.DeleteGroupTooltipLine2, "할당된 건물은 **운영 지구**을 잃게 됩니다." },
                { LocalizationKey.TypePickerTooltipLine1, "그룹의 **유형**을 변경합니다." },
                { LocalizationKey.TypePickerTooltipLine2, "**일반** 그룹은 모든 서비스 건물에 할당할 수 있습니다." },
                { LocalizationKey.TypePickerTooltipLine3, "다른 모든 유형은 일치하는 서비스 건물에만 사용할 수 있습니다." },
                { LocalizationKey.DeleteGroupConfirmMessage, "\"{NAME}\"이(가) {COUNT}개의 서비스 건물에 할당되어 있습니다.\n할당된 서비스 건물은 다시 도시 전역을 대상으로 서비스를 제공합니다." },
                { LocalizationKey.DeleteGroupDialogTitle, "지구 그룹을 삭제하시겠습니까?" },
                { LocalizationKey.DeleteGroupConfirm, "그룹 삭제" },
                { LocalizationKey.DeleteGroupCancel, "그룹 유지" },
                { LocalizationKey.RemoveMemberTooltip, "그룹에서 이 지구을 제거합니다." },
                { LocalizationKey.SelectDistrictsButton, "지구 선택" },
                { LocalizationKey.GroupColorTooltip, "그룹 색상" },
                { LocalizationKey.NameInputTooltip, "선택하여 이름을 편집할 수 있습니다." },
                { LocalizationKey.MetadataDistrictsTooltip, "지구" },
                { LocalizationKey.MetadataBuildingsTooltip, "배정된 건물" },
                { LocalizationKey.MetadataPopulationTooltip, "인구" },

                { LocalizationKey.ToggleTooltipTitle, "**지구 그룹**" },
                { LocalizationKey.ToggleTooltipBody, "지구 그룹을 만들어 서비스 건물에 할당하면 **운영 지구**이 자동으로 관리됩니다." },

                { LocalizationKey.SectionTooltipLine1, "서비스 건물은 **지구 그룹**에 할당할 수 있습니다." },
                { LocalizationKey.SectionTooltipLine2, "할당하면 그룹이 해당 건물의 **운영 지구**을 관리합니다." },
                { LocalizationKey.SectionTooltipLine3, "할당을 해제하면 **운영 지구**을 수동으로 관리합니다." },
                { LocalizationKey.SectionTooltipLine4, "참고: 할당을 변경한 후 정보 패널이 화면에 반영되기까지 몇 초 정도 걸릴 수 있습니다." },
                { LocalizationKey.SectionLabel, "지구 그룹" },
                { LocalizationKey.OperatingDistrictsLabel, "운영 지구" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "이 건물은 현재 지구 그룹에 할당되어 있습니다." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**지구** 할당은 **할당된 지구 그룹**이 관리합니다." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "**지구 그룹**에 **지구**가 없으면 이 건물은 **운영 범위** 내 모든 곳에 서비스를 제공합니다." },
                { LocalizationKey.UnassignOption, "할당 해제" },
                { LocalizationKey.UnassignTooltipDisabled, "할당된 그룹이 없습니다." },
                { LocalizationKey.UnassignTooltipEnabled, "현재 그룹 할당을 제거합니다." },
                { LocalizationKey.UnassignedLabel, "할당되지 않음" },
                { LocalizationKey.GroupSearchTitle, "지구 그룹 선택" },
                { LocalizationKey.SearchGroupsPlaceholder, "검색..." },
                { LocalizationKey.NoGroupsMatchSearch, "검색어와 일치하는 그룹이 없습니다." },
                { LocalizationKey.NoGroupsInSection, "그룹을 찾을 수 없습니다." },

                { LocalizationKey.TypeGeneric, "일반" },
                { LocalizationKey.TypePolice, "경찰" },
                { LocalizationKey.TypeFire, "화재" },
                { LocalizationKey.TypeHealthcare, "의료" },
                { LocalizationKey.TypeDeathcare, "장례" },
                { LocalizationKey.TypeGarbage, "쓰레기" },
                { LocalizationKey.TypeEducationElementary, "초등학교" },
                { LocalizationKey.TypeEducationHighSchool, "고등학교" },
                { LocalizationKey.TypeEducationCollege, "전문 대학" },
                { LocalizationKey.TypeEducationUniversity, "대학교" },
                { LocalizationKey.TypePost, "우편" },
                { LocalizationKey.TypeParks, "공원" },
                { LocalizationKey.TypeWelfare, "복지" },
            };
        }

        public void Unload() { }
    }
}

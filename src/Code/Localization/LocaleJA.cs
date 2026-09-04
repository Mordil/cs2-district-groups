using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleJA : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleJA(Setting setting)
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
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "メイン" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "オーバーレイ" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "開発者" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "デフォルト" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "トラブルシューティング" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "境界線の幅" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "オーバーレイに描画される特区の境界線の幅です。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "境界線の透明度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "オーバーレイに描画される特区の境界線の透明度です。\n\n0%で完全に不透明、100%で完全に透明になります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "シーンの彩度低下" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "グループのオーバーレイが表示されている間、他のシーン全体の彩度をどれだけ下げるかを設定します。\n\n0%ではシーンはそのまま変化せず、100%では完全なグレースケールになります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "塗りつぶしの彩度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "グループのオーバーレイの塗りつぶし範囲の彩度を設定します。\n\n100%ではグループの色がそのまま表示され、値が低いほどグレーに近づきます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "塗りつぶしの透明化を使用" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "グループのオーバーレイの塗りつぶし範囲の透明化を有効にします。\n\n無効にすると、オーバーレイは他のすべての表示を隠す不透明な単色で表示されます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "グループ名を表示" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "有効にすると、地図上のオーバーレイと共にグループ名が表示されます。\n\nパフォーマンスに影響する場合があります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "デバッグログを有効化" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "詳細なDebugレベルの情報をMODのログファイルに書き込みます。\n\nパフォーマンスに影響する場合があります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "デバッグ用MODデータをログに出力" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "MODの全状態(グループ、サービス施設など)をMODのログファイルに書き込みます。\n\n不具合を報告する際は、このログファイルを添付してください。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "不具合を報告" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "デバッグデータをログに記録した後、ブラウザでMODのGitHub Issueトラッカーを開きます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "更新頻度" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "特区の集計情報をUIで更新する間隔(秒)です。\n\n頻繁に更新すると、パフォーマンスに悪影響を及ぼす可能性があります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "すべての設定をリセット" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "すべてのMOD設定を初期値に戻します。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "すべてのMOD設定が初期値に戻ります。\r\n続行しますか?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "バージョン" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "インストールされているMODのバージョンです。\n\n不具合を報告する際に記載してください。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "リリースチャンネル" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "MODデータを削除" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "現在のセーブデータにMODが追加したすべての特区グループ、サービス施設の割り当て、オーバーレイ用リソースを削除します。" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "このセーブデータからすべての特区グループ、サービス施設の割り当て、オーバーレイデータが完全に削除されます。\r\nこの操作は元に戻せません。\r\n続行しますか?" },

                { LocalizationKey.PanelTitle, "District Groups" },
                { LocalizationKey.GroupsTabLabel, "グループ" },
                { LocalizationKey.AssignmentsTabLabel, "サービス施設" },
                { LocalizationKey.SelectTypeForAssignments, "サービスの種類を選択すると、該当するサービス施設が一覧表示されます。" },
                { LocalizationKey.NoServiceBuildingsMatchFilter, "このフィルターに一致するサービス施設がありません。" },
                { LocalizationKey.HideAssignedBuildingsLabel, "割り当て済みの建物を非表示" },
                { LocalizationKey.HideAssignedBuildingsTooltip, "すでに**特区グループ**に割り当てられているサービス施設を非表示にします。" },
                { LocalizationKey.NewGroupButton, "新規グループ" },
                { LocalizationKey.NewGroupButtonTooltip, "所属する特区がない新しいグループを追加します。" },
                { LocalizationKey.NewGroupDefaultName, "新規グループ {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "グループの一覧を**タイプ**で絞り込みます。" },
                { LocalizationKey.FilterTooltipAssignmentsLine1, "サービス施設の一覧を**タイプ**で絞り込みます。" },
                { LocalizationKey.AllGroupsLabel, "すべてのグループ" },
                { LocalizationKey.NoGroupsYet, "グループがまだありません。「新規グループ」ボタンで作成してください。" },
                { LocalizationKey.NoGroupsMatchFilter, "このフィルターに一致するグループがありません。" },
                { LocalizationKey.DisplayDistrictAreasLabel, "すべての特区を表示" },
                { LocalizationKey.ShowGroupOverlayLabel, "グループのオーバーレイを表示" },
                { LocalizationKey.ShowServiceBuildingsLabel, "サービス施設を表示" },

                { LocalizationKey.DeleteGroupTooltipLine1, "グループを完全に削除します。" },
                { LocalizationKey.DeleteGroupTooltipLine2, "割り当てられた施設は**稼働特区**を失います。" },
                { LocalizationKey.TypePickerTooltipLine1, "グループの**タイプ**を変更します。" },
                { LocalizationKey.TypePickerTooltipLine2, "**汎用**グループは、どのサービス施設にも割り当てることができます。" },
                { LocalizationKey.TypePickerTooltipLine3, "他のタイプは、対応するサービス施設にのみ割り当てることができます。" },
                { LocalizationKey.DeleteGroupConfirmMessage, "「{NAME}」は{COUNT}件のサービス施設に割り当てられています。\n割り当てられたサービス施設は、再び都市全体にサービスを提供するようになります。" },
                { LocalizationKey.DeleteGroupDialogTitle, "特区グループを削除しますか?" },
                { LocalizationKey.DeleteGroupConfirm, "グループを削除" },
                { LocalizationKey.DeleteGroupCancel, "グループを残す" },
                { LocalizationKey.RemoveMemberTooltip, "この特区をグループから削除します。" },
                { LocalizationKey.SelectDistrictsButton, "特区を選択" },
                { LocalizationKey.GroupColorTooltip, "グループの色" },
                { LocalizationKey.NameInputTooltip, "選択して名前を編集します。" },
                { LocalizationKey.MetadataDistrictsTooltip, "特区" },
                { LocalizationKey.MetadataBuildingsTooltip, "割り当てられた建物" },
                { LocalizationKey.MetadataPopulationTooltip, "人口" },

                { LocalizationKey.ToggleTooltipTitle, "**特区グループ**" },
                { LocalizationKey.ToggleTooltipBody, "特区をグループ化してサービス施設に割り当てることで、**稼働特区**を自動的に管理できるようにします。" },

                { LocalizationKey.SectionTooltipLine1, "サービス施設には**特区グループ**を割り当てることができます。" },
                { LocalizationKey.SectionTooltipLine2, "割り当てると、そのグループが施設の**稼働特区**を管理します。" },
                { LocalizationKey.SectionTooltipLine3, "割り当てを解除すると、**稼働特区**は手動で管理されます。" },
                { LocalizationKey.SectionTooltipLine4, "注記: 割り当てを変更した後、インフォパネルの表示が更新されるまで数秒かかる場合があります。" },
                { LocalizationKey.SectionLabel, "特区グループ" },
                { LocalizationKey.OperatingDistrictsLabel, "稼働特区" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "この建物は現在、特区グループに割り当てられています。" },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "**都市の特区**の割り当ては、**割り当てられた特区グループ**によって管理されます。" },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "**特区グループ**に**都市の特区**が含まれていない場合、この建物は**運用範囲内**のどこにでもサービスを提供します。" },
                { LocalizationKey.UnassignOption, "割り当て解除" },
                { LocalizationKey.UnassignTooltipDisabled, "グループが割り当てられていません。" },
                { LocalizationKey.UnassignTooltipEnabled, "現在のグループの割り当てを解除します。" },
                { LocalizationKey.UnassignedLabel, "未割り当て" },
                { LocalizationKey.GroupSearchTitle, "特区グループを選択" },
                { LocalizationKey.SearchGroupsPlaceholder, "検索..." },
                { LocalizationKey.NoGroupsMatchSearch, "検索条件に一致するグループがありません。" },
                { LocalizationKey.NoGroupsInSection, "グループが見つかりません。" },

                { LocalizationKey.TypeGeneric, "汎用" },
                { LocalizationKey.TypePolice, "警察" },
                { LocalizationKey.TypeFire, "火災" },
                { LocalizationKey.TypeHealthcare, "医療" },
                { LocalizationKey.TypeDeathcare, "葬儀" },
                { LocalizationKey.TypeGarbage, "ゴミ" },
                { LocalizationKey.TypeEducationElementary, "小学校" },
                { LocalizationKey.TypeEducationHighSchool, "高校" },
                { LocalizationKey.TypeEducationCollege, "単科大学" },
                { LocalizationKey.TypeEducationUniversity, "総合大学" },
                { LocalizationKey.TypePost, "郵便" },
                { LocalizationKey.TypeParks, "公園" },
                { LocalizationKey.TypeWelfare, "福祉" },
            };
        }

        public void Unload() { }
    }
}

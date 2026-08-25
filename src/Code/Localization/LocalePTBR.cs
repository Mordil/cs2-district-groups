using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocalePTBR : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocalePTBR(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "Grupos de Distritos" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Principal" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Sobreposição" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Desenvolvedor" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Padrão" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "Interface de Sobreposição" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Solução de Problemas" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Largura da borda" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Largura das linhas coloridas do contorno do grupo de distritos desenhadas no mapa." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacidade da borda" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacidade das linhas coloridas do contorno do grupo de distritos desenhadas no mapa.\n\n0% é totalmente transparente, 100% é totalmente opaco." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "Deslocamento de altura" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderHeightOffset)), "Um deslocamento em relação à altura do terreno no qual a sobreposição do grupo de distritos é desenhada." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Dessaturação da cena" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "O quanto o restante da cena é dessaturado enquanto a sobreposição do grupo está visível.\n\n0% deixa a cena inalterada, 100% fica em escala de cinza." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Saturação do preenchimento" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "O quão saturadas são as áreas de preenchimento coloridas do grupo de distritos.\n\n100% é a cor completa do grupo; valores menores esmaecem em direção ao cinza." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Ativar registro de depuração" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Grava entradas detalhadas de nível Debug no arquivo de log do mod.\n\nIsso pode afetar o desempenho." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Registrar dados de depuração do mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Grava todo o estado do mod (grupos, edifícios de serviço, etc.) no arquivo de log do mod.\n\nInclua o arquivo de log ao registrar um relatório de erro." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Registrar um erro" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Registra dados de depuração e depois abre o rastreador de problemas do GitHub do mod no seu navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Redefinir todas as configurações" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Redefine todas as configurações do mod para os valores padrão." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Todas as configurações do mod voltarão aos valores padrão.\r\nDeseja continuar?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Versão" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "A versão instalada do mod.\n\nInclua esta informação ao registrar um relatório de erro." },

                { LocalizationKey.PanelTitle, "Grupos de Distritos" },
                { LocalizationKey.NewGroupButton, "Novo Grupo" },
                { LocalizationKey.NewGroupButtonTooltip, "Adiciona um novo grupo sem distritos membros." },
                { LocalizationKey.NewGroupDefaultName, "Novo Grupo {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtra a lista de grupos pelo **tipo**." },
                { LocalizationKey.AllGroupsLabel, "Todos os Grupos" },
                { LocalizationKey.NoGroupsYet, "Ainda não há grupos. Crie um com o botão NOVO GRUPO." },
                { LocalizationKey.NoGroupsMatchFilter, "Nenhum grupo corresponde a este filtro." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Mostrar todos os distritos" },
                { LocalizationKey.ShowGroupOverlayLabel, "Mostrar sobreposição do grupo" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Exclui o grupo permanentemente." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Os edifícios atribuídos perderão seus **distritos em operação**." },
                { LocalizationKey.TypePickerTooltipLine1, "Altera o **tipo** do grupo." },
                { LocalizationKey.TypePickerTooltipLine2, "Grupos **Genéricos** podem ser atribuídos a qualquer edifício de serviço." },
                { LocalizationKey.TypePickerTooltipLine3, "Todos os outros tipos só estão disponíveis para edifícios de serviço correspondentes." },
                { LocalizationKey.DeleteGroupConfirmMessage, "\"{NAME}\" está atribuído a {COUNT} edifício(s) de serviço.\nO(s) edifício(s) de serviço atribuído(s) voltará(ão) a atender a cidade inteira." },
                { LocalizationKey.DeleteGroupDialogTitle, "Excluir Grupo de Distritos?" },
                { LocalizationKey.DeleteGroupConfirm, "Excluir grupo" },
                { LocalizationKey.DeleteGroupCancel, "Manter grupo" },
                { LocalizationKey.RemoveMemberTooltip, "Remove o distrito do grupo." },
                { LocalizationKey.SelectDistrictsButton, "Selecionar Distritos" },
                { LocalizationKey.GroupColorTooltip, "Cor do Grupo" },

                { LocalizationKey.ToggleTooltipTitle, "**GRUPOS DE DISTRITOS**" },
                { LocalizationKey.ToggleTooltipBody, "Crie grupos de distritos para atribuir a edifícios de serviço e gerenciar automaticamente os **distritos em operação**." },

                { LocalizationKey.SectionTooltipLine1, "Edifícios de serviço podem ser atribuídos a um **grupo de distritos**." },
                { LocalizationKey.SectionTooltipLine2, "Quando atribuído, o grupo gerenciará os **distritos em operação** do edifício." },
                { LocalizationKey.SectionTooltipLine3, "Quando não atribuído, os **distritos em operação** são gerenciados manualmente." },
                { LocalizationKey.SectionTooltipLine4, "OBSERVAÇÃO: O Painel de Informações pode levar alguns segundos para atualizar visualmente após a alteração da atribuição." },
                { LocalizationKey.SectionLabel, "GRUPO DE DISTRITOS" },
                { LocalizationKey.OperatingDistrictsLabel, "Distritos em Operação" },
                { LocalizationKey.UnassignOption, "Remover atribuição" },
                { LocalizationKey.UnassignTooltipDisabled, "Nenhum grupo está atribuído." },
                { LocalizationKey.UnassignTooltipEnabled, "Remove a atribuição de grupo atual." },
                { LocalizationKey.UnassignedLabel, "Não atribuído" },
                { LocalizationKey.GroupSearchTitle, "Selecionar Grupo de Distritos" },
                { LocalizationKey.SearchGroupsPlaceholder, "Pesquisar..." },
                { LocalizationKey.NoGroupsMatchSearch, "Nenhum grupo corresponde à sua pesquisa." },
                { LocalizationKey.NoGroupsInSection, "Nenhum grupo encontrado." },

                { LocalizationKey.TypeGeneric, "Genérico" },
                { LocalizationKey.TypePolice, "Polícia" },
                { LocalizationKey.TypeFire, "Incêndio" },
                { LocalizationKey.TypeHealthcare, "Sistema de Saúde" },
                { LocalizationKey.TypeDeathcare, "Assistência Funerária" },
                { LocalizationKey.TypeGarbage, "Lixo" },
                { LocalizationKey.TypeEducationElementary, "Escola Fundamental" },
                { LocalizationKey.TypeEducationHighSchool, "Escola de Ensino Médio" },
                { LocalizationKey.TypeEducationCollege, "Faculdade" },
                { LocalizationKey.TypeEducationUniversity, "Universidade" },
                { LocalizationKey.TypePost, "Correio" },
                { LocalizationKey.TypeParks, "Parques" },
                { LocalizationKey.TypeWelfare, "Previdência Social" },
            };
        }

        public void Unload() { }
    }
}

using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleFR : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleFR(Setting setting)
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
                { m_Setting.GetSettingsLocaleID(), "Groupes de quartiers" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabGeneral), "Général" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Superposition" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Développeur" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Par défaut" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionOverlay), "Interface de superposition" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Dépannage" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Épaisseur de la bordure" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Épaisseur des lignes de délimitation colorées des groupes de quartiers tracées sur la carte." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacité de la bordure" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderAlpha)), "Opacité des lignes de délimitation colorées des groupes de quartiers tracées sur la carte.\n\n0 % est entièrement transparent, 100 % est entièrement opaque." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Désaturation de la scène" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "À quel point le reste de la scène est désaturé lorsque la superposition des groupes est visible.\n\n0 % laisse la scène inchangée, 100 % correspond à un rendu en niveaux de gris." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Saturation du remplissage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "À quel point les zones de remplissage colorées des groupes de quartiers sont saturées.\n\n100 % correspond à la couleur pleine du groupe ; les valeurs plus faibles s'estompent vers le gris." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Transparence du remplissage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Affiche les zones de remplissage des groupes de quartiers avec de la transparence plutôt qu'une couleur pleine et entièrement opaque." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Activer la journalisation de débogage" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Écrit des entrées détaillées de niveau débogage dans le fichier journal du mod.\n\nCela peut affecter les performances." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Journaliser les données de débogage du mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Écrit l'état complet du mod (groupes, bâtiments de service, etc.) dans le fichier journal du mod.\n\nJoignez le fichier journal lorsque vous signalez un bug." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Signaler un bug" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Journalise les données de débogage, puis ouvre le suivi des tickets GitHub du mod dans votre navigateur." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Fréquence de mise à jour" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "Le nombre de secondes à attendre entre les mises à jour de l'interface pour les informations agrégées des quartiers.\n\nUne mise à jour fréquente peut avoir un impact négatif sur les performances." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Réinitialiser tous les paramètres" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Réinitialise tous les paramètres du mod à leurs valeurs par défaut." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Tous les paramètres du mod reviendront à leurs valeurs par défaut.\r\nVoulez-vous continuer ?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "La version installée du mod.\n\nIndiquez-la lorsque vous signalez un bug." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Canal de publication" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Supprimer les données du mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Supprime tous les groupes de quartiers, les affectations de bâtiments de service et les ressources de surimpression que le mod a ajoutés à la partie actuelle." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Tous les groupes de quartiers, les affectations de bâtiments de service et les données de surimpression seront définitivement supprimés de cette partie.\r\nCette action est irréversible.\r\nVoulez-vous continuer ?" },

                { LocalizationKey.PanelTitle, "Groupes de quartiers" },
                { LocalizationKey.NewGroupButton, "Nouveau groupe" },
                { LocalizationKey.NewGroupButtonTooltip, "Ajoute un nouveau groupe sans quartier membre." },
                { LocalizationKey.NewGroupDefaultName, "Nouveau groupe {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtre la liste des groupes par **type**." },
                { LocalizationKey.AllGroupsLabel, "Tous les groupes" },
                { LocalizationKey.NoGroupsYet, "Aucun groupe pour le moment. Créez-en un avec le bouton NOUVEAU GROUPE." },
                { LocalizationKey.NoGroupsMatchFilter, "Aucun groupe ne correspond à ce filtre." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Afficher tous les quartiers" },
                { LocalizationKey.ShowGroupOverlayLabel, "Afficher la superposition des groupes" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Supprime définitivement le groupe." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Les bâtiments assignés perdront leurs **quartiers d'exploitation**." },
                { LocalizationKey.TypePickerTooltipLine1, "Modifie le **type** du groupe." },
                { LocalizationKey.TypePickerTooltipLine2, "Les groupes **génériques** peuvent être assignés à n'importe quel bâtiment de service." },
                { LocalizationKey.TypePickerTooltipLine3, "Tous les autres types ne sont disponibles que pour les bâtiments de service correspondants." },
                { LocalizationKey.DeleteGroupConfirmMessage, "« {NAME} » est assigné à {COUNT} bâtiment(s) de service.\nLes bâtiments de service assignés desserviront de nouveau toute la ville." },
                { LocalizationKey.DeleteGroupDialogTitle, "Supprimer le groupe de quartiers ?" },
                { LocalizationKey.DeleteGroupConfirm, "Supprimer le groupe" },
                { LocalizationKey.DeleteGroupCancel, "Conserver le groupe" },
                { LocalizationKey.RemoveMemberTooltip, "Retire le quartier du groupe." },
                { LocalizationKey.SelectDistrictsButton, "Sélectionner des quartiers" },
                { LocalizationKey.GroupColorTooltip, "Couleur du groupe" },
                { LocalizationKey.NameInputTooltip, "Sélectionner pour modifier le nom." },
                { LocalizationKey.MetadataDistrictsTooltip, "Quartiers" },
                { LocalizationKey.MetadataBuildingsTooltip, "Bâtiments assignés" },
                { LocalizationKey.MetadataPopulationTooltip, "Population" },

                { LocalizationKey.ToggleTooltipTitle, "**GROUPES DE QUARTIERS**" },
                { LocalizationKey.ToggleTooltipBody, "Créez des groupes de quartiers à assigner aux bâtiments de service pour une gestion automatique des **quartiers d'exploitation**." },

                { LocalizationKey.SectionTooltipLine1, "Les bâtiments de service peuvent être assignés à un **groupe de quartiers**." },
                { LocalizationKey.SectionTooltipLine2, "Une fois assigné, le groupe gère les **quartiers d'exploitation** du bâtiment." },
                { LocalizationKey.SectionTooltipLine3, "Sans assignation, les **quartiers d'exploitation** sont gérés manuellement." },
                { LocalizationKey.SectionTooltipLine4, "REMARQUE : le panneau d'informations peut prendre quelques secondes avant de se mettre à jour visuellement après un changement d'assignation." },
                { LocalizationKey.SectionLabel, "GROUPE DE QUARTIERS" },
                { LocalizationKey.OperatingDistrictsLabel, "Quartiers d'exploitation" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Ce bâtiment est actuellement affecté à un groupe de quartiers." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "Les affectations de **quartier** seront gérées par le **groupe de quartiers affecté**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Si le **groupe de quartiers** ne comporte aucun **quartier**, ce bâtiment fournira ses services partout dans son **rayon d'action**." },
                { LocalizationKey.UnassignOption, "Désassigner" },
                { LocalizationKey.UnassignTooltipDisabled, "Aucun groupe n'est assigné." },
                { LocalizationKey.UnassignTooltipEnabled, "Retire l'assignation de groupe actuelle." },
                { LocalizationKey.UnassignedLabel, "Non assigné" },
                { LocalizationKey.GroupSearchTitle, "Sélectionner un groupe de quartiers" },
                { LocalizationKey.SearchGroupsPlaceholder, "Rechercher..." },
                { LocalizationKey.NoGroupsMatchSearch, "Aucun groupe ne correspond à votre recherche." },
                { LocalizationKey.NoGroupsInSection, "Aucun groupe trouvé." },

                { LocalizationKey.TypeGeneric, "Générique" },
                { LocalizationKey.TypePolice, "Police" },
                { LocalizationKey.TypeFire, "Incendie" },
                { LocalizationKey.TypeHealthcare, "Services médicaux" },
                { LocalizationKey.TypeDeathcare, "Soins mortuaires" },
                { LocalizationKey.TypeGarbage, "Déchets" },
                { LocalizationKey.TypeEducationElementary, "École primaire" },
                { LocalizationKey.TypeEducationHighSchool, "Lycée" },
                { LocalizationKey.TypeEducationCollege, "Établissement d'enseignement supérieur" },
                { LocalizationKey.TypeEducationUniversity, "Université" },
                { LocalizationKey.TypePost, "Poste" },
                { LocalizationKey.TypeParks, "Parcs et loisirs" },
                { LocalizationKey.TypeWelfare, "Aide sociale" },
            };
        }

        public void Unload() { }
    }
}

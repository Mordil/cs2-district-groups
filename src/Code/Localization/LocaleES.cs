using Colossal;
using System.Collections.Generic;

namespace DistrictGroups
{
    public class LocaleES : IDictionarySource
    {
        private readonly Setting m_Setting;

        public LocaleES(Setting setting)
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
                { m_Setting.GetOptionTabLocaleID(Setting.kTabOverlay), "Superposición" },
                { m_Setting.GetOptionTabLocaleID(Setting.kTabDeveloper), "Desarrollador" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDefault), "Predeterminado" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSectionDebug), "Solución de problemas" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderWidth)), "Ancho del borde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderWidth)), "Ancho de las líneas de límite de los distritos que se dibujan en la superposición." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayBorderTransparency)), "Transparencia del borde" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayBorderTransparency)), "Transparencia de las líneas de límite de los distritos que se dibujan en la superposición.\n\nEl 0% es totalmente opaco; el 100%, totalmente transparente." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Desaturación de la escena" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayDesaturationPercent)), "Cuánto se desatura el resto de la escena mientras la superposición de grupos está visible.\n\nEl 0% deja la escena intacta; el 100% la deja en escala de grises." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Saturación del relleno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillSaturationPercent)), "Cuán saturadas están las áreas de relleno de la superposición del grupo.\n\nEl 100% es el color completo del grupo; los valores más bajos se difuminan hacia el gris." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Usar transparencia de relleno" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayFillUseTransparency)), "Activa la transparencia en las áreas de relleno de la superposición del grupo.\n\nSi está desactivada, la superposición mostrará un color sólido y totalmente opaco que oculta todos los demás elementos visuales." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "Mostrar etiquetas de grupos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OverlayEnableGroupLabels)), "Si está activada, los nombres de los grupos se mostrarán con la superposición en el mapa.\n\nEsto puede afectar al rendimiento." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EnableDebugLogging)), "Activar registro de depuración" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EnableDebugLogging)), "Escribe entradas detalladas de nivel de depuración en el archivo de registro del mod.\n\nEsto puede afectar al rendimiento." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DumpDebugData)), "Registrar datos de depuración del mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.DumpDebugData)), "Escribe todo el estado del mod (grupos, edificios de servicio, etc.) en el archivo de registro del mod.\n\nAdjunta el archivo de registro al informar de un error." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.FileBug)), "Informar de un error" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.FileBug)), "Registra los datos de depuración y, después, abre el gestor de incidencias de GitHub del mod en tu navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshRateSeconds)), "Frecuencia de actualización" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshRateSeconds)), "El número de segundos que se espera entre actualizaciones de la interfaz para la información agregada de distritos.\n\nActualizar con frecuencia puede afectar negativamente al rendimiento." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ResetSettings)), "Restablecer todos los ajustes" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ResetSettings)), "Restablece todos los ajustes del mod a sus valores predeterminados." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.ResetSettings)), "Todos los ajustes del mod volverán a sus valores predeterminados.\r\n¿Deseas continuar?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ModVersion)), "Versión" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ModVersion)), "La versión instalada del mod.\n\nInclúyela al informar de un error." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.ReleaseChannel)), "Canal de versión" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.ReleaseChannel)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RemoveModData)), "Eliminar datos del mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RemoveModData)), "Elimina todos los grupos de distritos, asignaciones de edificios de servicio y recursos de superposición que el mod ha añadido a la partida actual." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.RemoveModData)), "Todos los grupos de distritos, asignaciones de edificios de servicio y datos de superposición se eliminarán permanentemente de esta partida.\r\nEsta acción no se puede deshacer.\r\n¿Deseas continuar?" },

                { LocalizationKey.PanelTitle, "Grupos de Distritos" },
                { LocalizationKey.GroupsTabLabel, "Grupos" },
                { LocalizationKey.AssignmentsTabLabel, "Edificios de servicio" },
                { LocalizationKey.SelectTypeForAssignments, "Selecciona un tipo de servicio para ver sus edificios de servicio." },
                { LocalizationKey.NoServiceBuildingsMatchFilter, "Ningún edificio de servicio coincide con este filtro." },
                { LocalizationKey.HideAssignedBuildingsLabel, "Ocultar edificios asignados" },
                { LocalizationKey.HideAssignedBuildingsTooltip, "Oculta los edificios de servicio que ya están asignados a un **grupo de distritos**." },
                { LocalizationKey.NewGroupButton, "Nuevo grupo" },
                { LocalizationKey.NewGroupButtonTooltip, "Añade un nuevo grupo sin distritos asignados." },
                { LocalizationKey.NewGroupDefaultName, "Nuevo grupo {NUMBER}" },
                { LocalizationKey.FilterTooltipLine1, "Filtra la lista de grupos por su **tipo**." },
                { LocalizationKey.AllGroupsLabel, "Todos los grupos" },
                { LocalizationKey.NoGroupsYet, "Aún no hay grupos. Crea uno con el botón NUEVO GRUPO." },
                { LocalizationKey.NoGroupsMatchFilter, "Ningún grupo coincide con este filtro." },
                { LocalizationKey.DisplayDistrictAreasLabel, "Mostrar todos los distritos" },
                { LocalizationKey.ShowGroupOverlayLabel, "Mostrar superposición de grupos" },
                { LocalizationKey.ShowServiceBuildingsLabel, "Mostrar edificios de servicio" },

                { LocalizationKey.DeleteGroupTooltipLine1, "Elimina el grupo de forma permanente." },
                { LocalizationKey.DeleteGroupTooltipLine2, "Los edificios asignados perderán sus **distritos operativos**." },
                { LocalizationKey.TypePickerTooltipLine1, "Cambia el **tipo** del grupo." },
                { LocalizationKey.TypePickerTooltipLine2, "Los grupos **genéricos** se pueden asignar a cualquier edificio de servicio." },
                { LocalizationKey.TypePickerTooltipLine3, "El resto de tipos solo están disponibles para los edificios de servicio correspondientes." },
                { LocalizationKey.DeleteGroupConfirmMessage, "«{NAME}» está asignado a {COUNT} edificio(s) de servicio.\nLos edificios de servicio asignados volverán a dar servicio a toda la ciudad." },
                { LocalizationKey.DeleteGroupDialogTitle, "¿Eliminar grupo de distritos?" },
                { LocalizationKey.DeleteGroupConfirm, "Eliminar grupo" },
                { LocalizationKey.DeleteGroupCancel, "Conservar grupo" },
                { LocalizationKey.RemoveMemberTooltip, "Elimina el distrito del grupo." },
                { LocalizationKey.SelectDistrictsButton, "Seleccionar distritos" },
                { LocalizationKey.GroupColorTooltip, "Color del grupo" },
                { LocalizationKey.NameInputTooltip, "Elige esto para editar el nombre." },
                { LocalizationKey.MetadataDistrictsTooltip, "Distritos" },
                { LocalizationKey.MetadataBuildingsTooltip, "Edificios asignados" },
                { LocalizationKey.MetadataPopulationTooltip, "Población" },

                { LocalizationKey.ToggleTooltipTitle, "**GRUPOS DE DISTRITOS**" },
                { LocalizationKey.ToggleTooltipBody, "Crea grupos de distritos para asignarlos a edificios de servicio y gestionar automáticamente sus **distritos operativos**." },

                { LocalizationKey.SectionTooltipLine1, "Los edificios de servicio se pueden asignar a un **grupo de distritos**." },
                { LocalizationKey.SectionTooltipLine2, "Cuando está asignado, el grupo gestiona los **distritos operativos** del edificio." },
                { LocalizationKey.SectionTooltipLine3, "Cuando no hay ninguno asignado, los **distritos operativos** se gestionan manualmente." },
                { LocalizationKey.SectionTooltipLine4, "NOTA: El panel de información puede tardar unos segundos en actualizarse visualmente tras cambiar la asignación." },
                { LocalizationKey.SectionLabel, "GRUPO DE DISTRITOS" },
                { LocalizationKey.OperatingDistrictsLabel, "Distritos operativos" },
                { LocalizationKey.ReadOnlySectionTooltipLine1, "Este edificio está actualmente asignado a un grupo de distritos." },
                { LocalizationKey.ReadOnlySectionTooltipLine2, "Las asignaciones de **distrito de la ciudad** las gestiona el **grupo de distritos asignado**." },
                { LocalizationKey.ReadOnlySectionTooltipLine3, "Si el **grupo de distritos** no tiene ningún **distrito de la ciudad**, este edificio prestará sus servicios en todas partes dentro de su **radio operativo**." },
                { LocalizationKey.UnassignOption, "Desasignar" },
                { LocalizationKey.UnassignTooltipDisabled, "No hay ningún grupo asignado." },
                { LocalizationKey.UnassignTooltipEnabled, "Elimina la asignación de grupo actual." },
                { LocalizationKey.UnassignedLabel, "Sin asignar" },
                { LocalizationKey.GroupSearchTitle, "Seleccionar grupo de distritos" },
                { LocalizationKey.SearchGroupsPlaceholder, "Buscar..." },
                { LocalizationKey.NoGroupsMatchSearch, "Ningún grupo coincide con tu búsqueda." },
                { LocalizationKey.NoGroupsInSection, "No se han encontrado grupos." },

                { LocalizationKey.TypeGeneric, "Genérico" },
                { LocalizationKey.TypePolice, "Policía" },
                { LocalizationKey.TypeFire, "Incendio" },
                { LocalizationKey.TypeHealthcare, "Sanidad" },
                { LocalizationKey.TypeDeathcare, "Funeraria" },
                { LocalizationKey.TypeGarbage, "Residuos" },
                { LocalizationKey.TypeEducationElementary, "Escuela primaria" },
                { LocalizationKey.TypeEducationHighSchool, "Escuela secundaria" },
                { LocalizationKey.TypeEducationCollege, "Escuela superior" },
                { LocalizationKey.TypeEducationUniversity, "Universidad" },
                { LocalizationKey.TypePost, "Correo" },
                { LocalizationKey.TypeParks, "Parques" },
                { LocalizationKey.TypeWelfare, "Oficinas de seguridad social" },
            };
        }

        public void Unload() { }
    }
}

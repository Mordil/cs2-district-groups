import { useValue } from "cs2/api"
import { Scrollable } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { useTranslation } from "../locale"
import { serviceBuildings$ } from "./bindings"
import css from "./AssignmentsTab.module.scss"

const kGenericType = 0

const emptyTextStyle = { fontSize: "var(--fontSizeM)" }

interface AssignmentsTabProps {
    filterType: number
    className?: string
}

export const AssignmentsTab = ({ filterType, className }: AssignmentsTabProps) => {
    const t = useTranslation()
    const buildings = useValue(serviceBuildings$)

    return (
        <Scrollable
            vertical={true}
            trackVisibility="reserve"
            className={className}
        >
            {filterType === kGenericType && (
                <div style={emptyTextStyle}>
                    {t("selectTypeForAssignments")}
                </div>
            )}

            {filterType !== kGenericType && buildings.length === 0 && (
                <div style={emptyTextStyle}>
                    {t("noServiceBuildingsMatchFilter")}
                </div>
            )}

            {filterType !== kGenericType &&
                buildings.map((building) => (
                    <div key={entityKey(building.entity)} className={css.row}>
                        {building.name}
                    </div>
                ))}
        </Scrollable>
    )
}

import { trigger, useValue } from "cs2/api"
import { LocalizedString } from "cs2/l10n"
import { FormattedParagraphs, Scrollable } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"
import { MouseEvent } from "react"
import mod from "../../mod.json"
import { eligibleGroups, GroupSelector } from "../components/groupSelector"
import { gameIconSrc } from "../components/icons"
import { VC, VF, VT } from "../components/vanilla"
import { kGenericType } from "../constants"
import { useTranslation } from "../locale"
import { logger } from "../log"
import { Group, ServiceBuilding } from "../types"
import { groups$, serviceBuildings$ } from "./bindings"
import { markdownRenderer } from "../shared"
import css from "./AssignmentsTab.module.scss"

const emptyTextStyle = { fontSize: "var(--fontSizeM)" }

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

interface AssignmentsTabProps {
    filterType: number
    hideAssigned: boolean
    className?: string
}

interface BuildingRowProps {
    building: ServiceBuilding
    groups: Group[]
    onSelect: (building: ServiceBuilding, group: Entity) => void
    onUnassign: (building: ServiceBuilding) => void
}

const BuildingRow = ({ building, groups, onSelect, onUnassign }: BuildingRowProps) => {
    const t = useTranslation()
    const hasAssetName = Boolean(building.assetNameId || building.assetName)

    const sectionTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                t("sectionTooltipLine1"),
                t("sectionTooltipLine2"),
                t("sectionTooltipLine3"),
            ]}
        />
    )

    return (
        <div className={css.row}>
            <div className={css.buildingDetails}>
                <div className={css.buildingName}>{building.name}</div>

                {hasAssetName && (
                    <div className={css.assetName}>
                        <LocalizedString
                            id={building.assetNameId}
                            fallback={building.assetName}
                        />
                    </div>
                )}
            </div>

            <GroupSelector
                buildingType={building.type}
                candidates={eligibleGroups(groups, building.type, building.assignedGroup)}
                hasAssignment={building.hasAssignment}
                assignedGroupName={building.assignedGroupName}
                onSelect={(group) => onSelect(building, group)}
                onUnassign={() => onUnassign(building)}
                tooltip={sectionTooltip}
                className={css.groupSelector}
            />
        </div>
    )
}

export const AssignmentsTab = ({ filterType, hideAssigned, className }: AssignmentsTabProps) => {
    const t = useTranslation()
    const buildings = useValue(serviceBuildings$)
    const groups = useValue(groups$)

    const onSelect = (building: ServiceBuilding, group: Entity) => {
        logger.info(`Assign group clicked; building:${entityKey(building.entity)} group:${entityKey(group)}`)
        trigger(mod.id, "assignBuildingGroup", building.entity, group)
    }

    const onUnassign = (building: ServiceBuilding) => {
        logger.info(`Unassign group clicked; building:${entityKey(building.entity)}`)
        trigger(mod.id, "unassignBuildingGroup", building.entity)
    }

    // filter() already copies the array, so sorting the result in place is safe.
    const displayedBuildings = buildings
        .filter((building) => !hideAssigned || !building.hasAssignment)
        .sort((a, b) => a.name.localeCompare(b.name))

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

            {filterType !== kGenericType && displayedBuildings.length === 0 && (
                <div style={emptyTextStyle}>
                    {t("noServiceBuildingsMatchFilter")}
                </div>
            )}

            {filterType !== kGenericType &&
                displayedBuildings.map((building) => (
                    <BuildingRow
                        key={entityKey(building.entity)}
                        building={building}
                        groups={groups}
                        onSelect={onSelect}
                        onUnassign={onUnassign}
                    />
                ))}
        </Scrollable>
    )
}

import { ReactNode, useState } from "react"

import { LocalizedNumber, LocalizedString, Unit } from "cs2/l10n"
import { Icon, Scrollable } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { glyphIconSrc } from "../../components/icons"
import { VC, VF, VT } from "../../components/vanilla"
import { kUnknownEfficiency, kNoValue } from "../../constants"
import { AssignedBuilding, Group } from "../../types"
import { VanillaLocale } from "../../utils/locale"
import { logger } from "../../utils/log"

import css from "./index.module.scss"

const kTable = VT.table
const kTableRow = VT.tableRow

// The columns the building list can be ranked by.
enum BuildingsColumn {
    Building,
    Efficiency,
}

interface ColumnDef {
    id: BuildingsColumn
    label: ReactNode
    widthClass: string
    alignClass: string
    descendingFirst: boolean
    compare: (a: AssignedBuilding, b: AssignedBuilding) => number
    renderValue: (building: AssignedBuilding) => ReactNode
}

let lastSortColumn = BuildingsColumn.Building
let lastAscending = true

interface BuildingsTabProps {
    group: Group
    className?: string
}

// Lists the service buildings assigned to the group as a sortable table.
export const BuildingsTab = ({ group, className }: BuildingsTabProps) => {
    const [sortColumn, setSortColumn] = useState(lastSortColumn)
    const [ascending, setAscending] = useState(lastAscending)

    const columns: ColumnDef[] = [
        {
            id: BuildingsColumn.Building,
            label: (
                <LocalizedString
                    id={VanillaLocale.buildingsColumn.id}
                    fallback={VanillaLocale.buildingsColumn.fallback}
                />
            ),
            widthClass: kTable.cellWide,
            alignClass: kTable.alignLeft,
            descendingFirst: false,
            compare: (a, b) => a.name.localeCompare(b.name),
            renderValue: (building) => <div className={css.name}>{building.name}</div>,
        },
        {
            id: BuildingsColumn.Efficiency,
            label: (
                <LocalizedString
                    id={VanillaLocale.efficiencyColumn.id}
                    fallback={VanillaLocale.efficiencyColumn.fallback}
                />
            ),
            widthClass: kTable.cellDouble,
            alignClass: kTable.alignRight,
            descendingFirst: true,
            compare: (a, b) => a.efficiency - b.efficiency,
            renderValue: (building) =>
                building.efficiency === kUnknownEfficiency ? (
                    kNoValue
                ) : (
                    <LocalizedNumber value={building.efficiency} unit={Unit.Percentage} />
                ),
        },
    ]

    const activeColumn = columns.find((column) => column.id === sortColumn) ?? columns[0]
    const buildings = [...group.buildings].sort(
        (a, b) => (ascending ? 1 : -1) * activeColumn.compare(a, b)
    )

    const sortBy = (column: ColumnDef) => {
        const nextAscending = column.id === sortColumn ? !ascending : !column.descendingFirst
        logger.info(
            `Buildings column sorted; column:${BuildingsColumn[column.id]} ascending:${nextAscending}`
        )
        lastSortColumn = column.id
        lastAscending = nextAscending
        setSortColumn(column.id)
        setAscending(nextAscending)
    }

    const sortIndicator = (
        <Icon
            tinted={true}
            className={kTable.sortIndicator}
            src={glyphIconSrc(ascending ? "ThickStrokeArrowDown" : "ThickStrokeArrowUp")}
        />
    )

    // Every row ends on a spacer, holding a column open for per-building actions.
    const actionCell = <div className={kTable.cellSingle} />

    const cells = (render: (column: ColumnDef) => ReactNode) =>
        columns.map((column) => (
            <div key={column.id} className={`${column.widthClass} ${column.alignClass}`}>
                {render(column)}
            </div>
        ))

    return (
        <div className={`${css.table} ${className ?? ""}`}>
            <div className={`${kTable.legends} ${css.columnHeaders}`}>
                {cells((column) => (
                    <VC.Button
                        disableHint={true}
                        focusKey={VF.FOCUS_DISABLED}
                        className={kTable.button}
                        onSelect={() => sortBy(column)}
                    >
                        <div className={kTable.buttonLabel}>{column.label}</div>
                        {column.id === sortColumn && sortIndicator}
                    </VC.Button>
                ))}
                {actionCell}
            </div>

            <Scrollable vertical={true} trackVisibility="reserve" className={css.list}>
                {buildings.map((building) => (
                    <div key={entityKey(building.entity)} className={kTableRow.transportationLineItem}>
                        <div className={kTableRow.container}>
                            {cells((column) => column.renderValue(building))}
                            {actionCell}
                        </div>
                    </div>
                ))}
            </Scrollable>
        </div>
    )
}

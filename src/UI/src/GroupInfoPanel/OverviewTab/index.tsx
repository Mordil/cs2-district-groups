import { ReactNode, useState } from "react"

import { LocalizedNumber, LocalizedString, Unit } from "cs2/l10n"
import { Icon, Scrollable } from "cs2/ui"
import { entityKey } from "cs2/utils"

import { glyphIconSrc } from "../../components/icons"
import { ThresholdValue } from "../../components/ThresholdValue"
import { VC, VF, VT } from "../../components/vanilla"
import { DistrictMember, Group } from "../../types"
import { VanillaLocale, happinessThreshold, wealthThreshold } from "../../utils/locale"
import { logger } from "../../utils/log"

import css from "./index.module.scss"

const kTable = VT.table
const kTableRow = VT.tableRow

// The columns the district list can be ranked by.
enum OverviewColumn {
    District,
    Population,
    Happiness,
    Wealth,
}

interface ColumnDef {
    id: OverviewColumn
    label: ReactNode
    widthClass: string
    alignClass: string
    descendingFirst: boolean
    compare: (a: DistrictMember, b: DistrictMember) => number
    renderValue: (member: DistrictMember) => ReactNode
    renderTotal: (group: Group) => ReactNode
}

let lastSortColumn = OverviewColumn.District
let lastAscending = true

interface OverviewTabProps {
    group: Group
    className?: string
}

// Lists the districts that belong to the group as a sortable table.
export const OverviewTab = ({ group, className }: OverviewTabProps) => {
    const [sortColumn, setSortColumn] = useState(lastSortColumn)
    const [ascending, setAscending] = useState(lastAscending)

    const columns: ColumnDef[] = [
        {
            id: OverviewColumn.District,
            label: (
                <LocalizedString
                    id={VanillaLocale.districtsColumn.id}
                    fallback={VanillaLocale.districtsColumn.fallback}
                />
            ),
            widthClass: kTable.cellWide,
            alignClass: kTable.alignLeft,
            descendingFirst: false,
            compare: (a, b) => a.name.localeCompare(b.name),
            renderValue: (member) => <div className={css.name}>{member.name}</div>,
            renderTotal: () => (
                <div className={css.name}>
                    <LocalizedString id={VanillaLocale.total.id} fallback={VanillaLocale.total.fallback} />
                </div>
            ),
        },
        {
            id: OverviewColumn.Population,
            label: (
                <LocalizedString
                    id={VanillaLocale.populationColumn.id}
                    fallback={VanillaLocale.populationColumn.fallback}
                />
            ),
            widthClass: kTable.cellDouble,
            alignClass: kTable.alignRight,
            descendingFirst: true,
            compare: (a, b) => a.population - b.population,
            renderValue: (member) => <LocalizedNumber value={member.population} unit={Unit.Integer} />,
            renderTotal: (total) => <LocalizedNumber value={total.population} unit={Unit.Integer} />,
        },
        {
            id: OverviewColumn.Happiness,
            label: (
                <LocalizedString
                    id={VanillaLocale.happinessColumn.id}
                    fallback={VanillaLocale.happinessColumn.fallback}
                />
            ),
            widthClass: kTable.cellDouble,
            alignClass: kTable.alignRight,
            descendingFirst: true,
            compare: (a, b) => a.happiness - b.happiness,
            renderValue: (member) => <ThresholdValue label={happinessThreshold(member.happiness)} />,
            renderTotal: (total) => <ThresholdValue label={happinessThreshold(total.happiness)} />,
        },
        {
            id: OverviewColumn.Wealth,
            label: (
                <LocalizedString
                    id={VanillaLocale.wealthColumn.id}
                    fallback={VanillaLocale.wealthColumn.fallback}
                />
            ),
            widthClass: kTable.cellDouble,
            alignClass: kTable.alignRight,
            descendingFirst: true,
            compare: (a, b) => a.wealth - b.wealth,
            renderValue: (member) => <ThresholdValue label={wealthThreshold(member.wealth)} />,
            renderTotal: (total) => <ThresholdValue label={wealthThreshold(total.wealth)} />,
        },
    ]

    const activeColumn = columns.find((column) => column.id === sortColumn) ?? columns[0]
    const members = [...group.members].sort(
        (a, b) => (ascending ? 1 : -1) * activeColumn.compare(a, b)
    )

    const sortBy = (column: ColumnDef) => {
        const nextAscending = column.id === sortColumn ? !ascending : !column.descendingFirst
        logger.info(
            `Overview column sorted; column:${OverviewColumn[column.id]} ascending:${nextAscending}`
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

    // Every row ends on a spacer, holding a column open for per-district actions.
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
                        className={`${kTable.button} ${css.columnHeaderButton}`}
                        onSelect={() => sortBy(column)}
                    >
                        <div className={kTable.buttonLabel}>{column.label}</div>
                        {column.id === sortColumn && sortIndicator}
                    </VC.Button>
                ))}
                {actionCell}
            </div>

            <div className={`${kTableRow.container} ${css.totalRow}`}>
                {cells((column) => column.renderTotal(group))}
                {actionCell}
            </div>

            <Scrollable vertical={true} trackVisibility="reserve" className={css.list}>
                {members.map((member) => (
                    <div key={entityKey(member.entity)} className={kTableRow.transportationLineItem}>
                        <div className={kTableRow.container}>
                            {cells((column) => column.renderValue(member))}
                            {actionCell}
                        </div>
                    </div>
                ))}
            </Scrollable>
        </div>
    )
}

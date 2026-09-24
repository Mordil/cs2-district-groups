import { CSSProperties, MouseEvent, ReactNode, useState } from "react"

import { camera } from "cs2/bindings"
import { LocalizedString } from "cs2/l10n"
import { Icon, Scrollable, Tooltip } from "cs2/ui"
import { Entity, entityKey } from "cs2/utils"

import { VanillaLocale } from "../utils/locale"
import { logger } from "../utils/log"

import { gameIconSrc, glyphIconSrc } from "./icons"
import { VC, VF, VT } from "./vanilla"

import css from "./DataTable.module.scss"

const kTable = VT.table
const kTableRow = VT.tableRow

const removeButtonStyle = { "height": "24rem", "width": "24rem" } as CSSProperties

// How a cell that isn't a figure reads: a name ellipsizes in the wide cell, text wraps in a double.
type CellLayout = "name" | "text"

const kLayoutClasses: Record<CellLayout, string> = {
    name: `${kTable.cellWide} ${kTable.alignLeft}`,
    text: `${kTable.cellDouble} ${kTable.alignLeft}`,
}

const kContentClasses: Record<CellLayout, string> = {
    name: css.name,
    text: css.text,
}

// One column of a DataTable, read off a row of type T.
export interface DataColumn<T> {
    // Stable across a group's type changing, so a chosen sort survives the column list swapping under it
    id: string
    label: ReactNode
    // A column whose largest value is its most interesting one opens sorted downward
    descendingFirst?: boolean
    // Left out for a figure, which takes a right-aligned double cell
    layout?: CellLayout
    compare: (a: T, b: T) => number
    render: (row: T) => ReactNode
    // What the totals row reads out, where that differs from a row's own value
    renderTotal?: (total: T) => ReactNode
    // What the column was worked out from, shown on the heading itself
    headerTooltip?: (total: T | undefined) => ReactNode
}

// The sort each table was last left on, so reopening a panel reads the way the player left it.
const lastSort: Record<string, { column: string; ascending: boolean }> = {}

const stopMouseDown = (e: MouseEvent) => {
    e.preventDefault()
    e.stopPropagation()
}

interface DataTableProps<T> {
    // Names the table's remembered sort; no two tables share one
    id: string
    columns: DataColumn<T>[]
    rows: T[]
    // The row's subject, which keys the row and is what its focus action centres the camera on
    entityOf: (row: T) => Entity
    // The row pinned above the list, already summed
    total?: T
    onRemove?: (row: T) => void
    removeTooltip?: ReactNode
    className?: string
}

// A sortable table of rows, each ending on the actions that focus and remove its subject.
export const DataTable = <T,>({
    id,
    columns,
    rows,
    entityOf,
    total,
    onRemove,
    removeTooltip,
    className,
}: DataTableProps<T>) => {
    const remembered = lastSort[id]
    const [sortColumn, setSortColumn] = useState(remembered?.column ?? columns[0].id)
    const [ascending, setAscending] = useState(remembered?.ascending ?? true)

    const activeColumn = columns.find((column) => column.id === sortColumn) ?? columns[0]
    const sorted = [...rows].sort((a, b) => (ascending ? 1 : -1) * activeColumn.compare(a, b))

    const sortBy = (column: DataColumn<T>) => {
        const nextAscending = column.id === sortColumn ? !ascending : !column.descendingFirst
        logger.info(`Table column sorted; table:${id} column:${column.id} ascending:${nextAscending}`)
        lastSort[id] = { column: column.id, ascending: nextAscending }
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

    // Every row ends on a spacer, holding a column open for the per-row actions.
    const actionCell = <div className={`${kTable.cellDouble} ${css.actionCell}`} />

    const cellClass = (column: DataColumn<T>) =>
        column.layout ? kLayoutClasses[column.layout] : `${kTable.cellDouble} ${kTable.alignRight}`

    // A heading fills its own cell, since the sort button it holds is what has to stay clickable across it.
    const headerCells = () =>
        columns.map((column) => {
            const label = <div className={`${kTable.buttonLabel} ${css.columnHeaderLabel}`}>{column.label}</div>

            return (
                <div key={column.id} className={cellClass(column)}>
                    <VC.Button
                        disableHint={true}
                        focusKey={VF.FOCUS_DISABLED}
                        className={`${kTable.button} ${css.columnHeaderButton}`}
                        onSelect={() => sortBy(column)}
                    >
                        {column.headerTooltip ? (
                            <Tooltip tooltip={column.headerTooltip(total)}>{label}</Tooltip>
                        ) : (
                            label
                        )}

                        {column.id === activeColumn.id && sortIndicator}
                    </VC.Button>
                </div>
            )
        })

    // A name or text cell wraps its content in one more element to ellipsize or wrap against.
    const bodyCells = (render: (column: DataColumn<T>) => ReactNode) =>
        columns.map((column) => (
            <div key={column.id} className={cellClass(column)}>
                {column.layout ? (
                    <div className={kContentClasses[column.layout]}>{render(column)}</div>
                ) : (
                    render(column)
                )}
            </div>
        ))

    return (
        <div className={`${css.table} ${className ?? ""}`}>
            <div className={`${kTable.legends} ${css.columnHeaders}`}>
                {headerCells()}
                {actionCell}
            </div>

            {total !== undefined && (
                <div className={`${kTableRow.container} ${css.totalRow}`}>
                    {bodyCells((column) => (column.renderTotal ?? column.render)(total))}
                    {actionCell}
                </div>
            )}

            <Scrollable vertical={true} trackVisibility="reserve" className={css.list}>
                {sorted.map((row) => (
                    <div key={entityKey(entityOf(row))} className={kTableRow.transportationLineItem}>
                        <div className={kTableRow.container}>
                            {bodyCells((column) => column.render(row))}
                            <RowActions
                                entity={entityOf(row)}
                                table={id}
                                onRemove={onRemove && (() => onRemove(row))}
                                removeTooltip={removeTooltip}
                            />
                        </div>
                    </div>
                ))}
            </Scrollable>
        </div>
    )
}

interface RowActionsProps {
    entity: Entity
    table: string
    onRemove?: () => void
    removeTooltip?: ReactNode
}

const RowActions = ({ entity, table, onRemove, removeTooltip }: RowActionsProps) => (
    <div className={`${kTable.cellDouble} ${css.actionCell}`}>
        <Tooltip
            tooltip={
                <LocalizedString
                    id={VanillaLocale.focusTooltip.id}
                    fallback={VanillaLocale.focusTooltip.fallback}
                />
            }
        >
            <VC.IconButton
                tinted={false}
                focusKey={VF.FOCUS_DISABLED}
                theme={VT.actionButton}
                src={gameIconSrc("MapMarker")}
                onSelect={() => {
                    logger.info(`Table row focused; table:${table} entity:${entityKey(entity)}`)
                    camera.focusEntity(entity)
                }}
                onMouseDown={stopMouseDown}
            />
        </Tooltip>

        {onRemove && (
            <Tooltip tooltip={removeTooltip}>
                <div className={css.deleteButtonHover}>
                    <VC.IconButton
                        tinted={true}
                        focusKey={VF.FOCUS_DISABLED}
                        src={glyphIconSrc("Trash")}
                        className={VT.districtsSection.deleteButton}
                        style={removeButtonStyle}
                        onSelect={onRemove}
                        onMouseDown={stopMouseDown}
                    />
                </div>
            </Tooltip>
        )}
    </div>
)

interface EmptyTableProps {
    message: string
    className?: string
}

// Stands in for the table when the group has nothing for it to list.
export const EmptyTable = ({ message, className }: EmptyTableProps) => (
    <div className={`${css.empty} ${className ?? ""}`}>{message}</div>
)

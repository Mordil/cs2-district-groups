import { trigger, useValue } from "cs2/api"
import { Button, FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { MouseEvent, useState } from "react"
import mod from "../../mod.json"
import { Checkbox } from "../components/Checkbox"
import { TypeFilterPicker } from "../components/TypePicker"
import { VC, VF, VT } from "../components/vanilla"
import { useTypeLabels } from "../constants"
import { useTranslation } from "../locale"
import css from "./index.module.scss"
import { areasVisible$, groups$, showOverlay$ } from "./bindings"
import { markdownRenderer } from "../shared"
import { logger } from "../log"
import { GroupCard } from "./GroupCard"

export { groups$, areaToolActive$, overlayVisible$ } from "./bindings"

// temporarily persisted value between UI mounting; 0 = Generic
let lastFilterType = 0

interface GroupManagementPanelProps {
    onClose: () => void
}

export const GroupManagementPanel = ({ onClose }: GroupManagementPanelProps) => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [filterType, setFilterType] = useState(lastFilterType)
    const groups = useValue(groups$)
    const areasVisible = useValue(areasVisible$)
    const showOverlay = useValue(showOverlay$)

    const filterTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("filterTooltipLine1")]}
            nonInline
        />
    )

    // Groups matching the filtered type, in creation order (the binding's own order).
    const displayedGroups = groups.filter((g) => g.type === filterType)

    const onFilterChange = (type: number) => {
        logger.info(`Filter changed; type:${type}`)
        lastFilterType = type
        setFilterType(type)
        trigger(mod.id, "setOverlayFilter", type)
    }

    const onCreateGroup = () => {
        logger.info("New group clicked;")
        // groups.length (not displayedGroups.length) so the suggested name
        // reflects every group, regardless of the active filter.
        trigger(mod.id, "createGroup", t("newGroupDefaultName", { number: groups.length + 1 }), filterType)
    }

    const onAreasVisibleChange = (checked: boolean) => {
        logger.info(`Areas visible toggled; visible:${checked}`)
        trigger(mod.id, "setAreasVisible", checked)
    }

    const onShowOverlayChange = (checked: boolean) => {
        logger.info(`Show group overlay toggled; show:${checked}`)
        trigger(mod.id, "setShowOverlay", checked)
    }

    return (
        <div className={css.panel}>
            <div className={css.header}>
                <span className={css.title}>{t("panelTitle")}</span>
                <VC.IconButton
                    tinted={true}
                    focusKey={VF.FOCUS_DISABLED}
                    src={VT.panel.closeIcon}
                    theme={VT.roundIconButton}
                    className={VT.panel.closeButton}
                    onSelect={onClose}
                    onMouseDown={(e: MouseEvent) => {
                        e.preventDefault()
                        e.stopPropagation()
                    }}
                />
            </div>

            <div className={css.actionSection}>
                <TypeFilterPicker
                    value={filterType}
                    onChange={onFilterChange}
                    labels={typeLabels}
                    allLabel={null}
                    tooltip={filterTooltip}
                />

                <Tooltip tooltip={t("newGroupButtonTooltip")}>
                    <Button
                        variant="primary"
                        className={css.newGroupButton}
                        onSelect={onCreateGroup}
                    >
                        {t("newGroupButton")}
                    </Button>
                </Tooltip>
            </div>

            <Scrollable
                vertical={true}
                trackVisibility="reserve"
                className={css.list}
            >
                {groups.length === 0 && (
                    <div>{t("noGroupsYet")}</div>
                )}
                {groups.length > 0 && displayedGroups.length === 0 && (
                    <div>{t("noGroupsMatchFilter")}</div>
                )}
                {displayedGroups.map((group) => (
                    <GroupCard
                        key={entityKey(group.entity)}
                        group={group}
                    />
                ))}
            </Scrollable>

            <div className={css.footer}>
                <Checkbox
                    checked={showOverlay}
                    onChange={onShowOverlayChange}
                    label={t("showGroupOverlayLabel")}
                    className={css.areasToggleRow}
                />
                <Checkbox
                    checked={areasVisible}
                    onChange={onAreasVisibleChange}
                    label={t("displayDistrictAreasLabel")}
                    className={css.areasToggleRow}
                />
            </div>
        </div>
    )
}

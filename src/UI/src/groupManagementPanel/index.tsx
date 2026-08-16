import { trigger, useValue } from "cs2/api"
import { FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { useState } from "react"
import mod from "../../mod.json"
import { Checkbox } from "../components/Checkbox"
import { kAllTypes, TypeFilterPicker } from "../components/TypePicker"
import { useTypeLabels } from "../constants"
import { useTranslation } from "../locale"
import css from "./index.module.scss"
import { styles } from "./styles"
import { areasVisible$, groups$, showOverlay$ } from "./bindings"
import { markdownRenderer } from "../shared"
import { logger } from "../log"
import { GroupCard } from "./GroupCard"

// Re-exported: modMenuButton's own tooltip needs the live group count.
export { groups$ } from "./bindings"

// temporarily persisted value between UI mounting
let lastFilterType = kAllTypes

export const GroupManagementPanel = () => {
    const t = useTranslation()
    const typeLabels = useTypeLabels()
    const [filterType, setFilterType] = useState(lastFilterType)
    const groups = useValue(groups$)
    const areasVisible = useValue(areasVisible$)
    const showOverlay = useValue(showOverlay$)

    const filterTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[t("filterTooltipLine1"), t("filterTooltipLine2")]}
        />
    )

    // "All Types" keeps creation order (the binding's own order); a specific
    // type filters down to just that type, still in creation order.
    const displayedGroups = filterType === kAllTypes ? groups : groups.filter((g) => g.type === filterType)

    const onFilterChange = (type: number) => {
        logger.info(`Filter changed; type:${type}`)
        lastFilterType = type
        setFilterType(type)
        trigger(mod.id, "setOverlayFilter", type)
    }

    const onCreateGroup = () => {
        logger.info("New group clicked;")
        // "All Groups" (kAllTypes) has no real type to inherit, so new
        // groups created under that filter default to Generic.
        const newGroupType = filterType === kAllTypes ? 0 : filterType
        // groups.length (not displayedGroups.length) so the suggested name
        // reflects every group, regardless of the active filter.
        trigger(mod.id, "createGroup", t("newGroupDefaultName", { number: groups.length + 1 }), newGroupType)
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
        <>
            <div style={styles.panelHeader}>
                <div style={styles.headerRow}>
                    <div style={styles.header}>{t("panelTitle")}</div>
                    <div style={{ display: "flex", alignItems: "center" }}>
                        <Tooltip tooltip={t("newGroupButtonTooltip")}>
                            <button
                                className={css.newGroupButton}
                                style={styles.newGroupButton}
                                onClick={onCreateGroup}
                            >
                                {t("newGroupButton")}
                            </button>
                        </Tooltip>
                        <TypeFilterPicker
                            value={filterType}
                            onChange={onFilterChange}
                            labels={typeLabels}
                            allLabel={t("allGroupsLabel")}
                            tooltip={filterTooltip}
                        />
                    </div>
                </div>
            </div>

            <div style={styles.panelBody}>
                <Scrollable
                    vertical={true}
                    trackVisibility={displayedGroups.length > 0 ? "always" : "scrollable"}
                    style={styles.listArea}
                >
                    {groups.length === 0 && (
                        <div style={styles.subtle}>{t("noGroupsYet")}</div>
                    )}
                    {groups.length > 0 && displayedGroups.length === 0 && (
                        <div style={styles.subtle}>{t("noGroupsMatchFilter")}</div>
                    )}
                    {displayedGroups.map((group) => (
                        <GroupCard
                            key={entityKey(group.entity)}
                            group={group}
                        />
                    ))}
                </Scrollable>

                <div style={styles.divider} />
                <Checkbox
                    checked={showOverlay}
                    onChange={onShowOverlayChange}
                    label={t("showGroupOverlayLabel")}
                    style={styles.areasToggleRow}
                />
                <Checkbox
                    checked={areasVisible}
                    onChange={onAreasVisibleChange}
                    label={t("displayDistrictAreasLabel")}
                    style={styles.areasToggleRow}
                />
            </div>
        </>
    )
}

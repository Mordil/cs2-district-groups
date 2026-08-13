import { trigger, useValue } from "cs2/api"
import { FormattedParagraphs, Scrollable, Tooltip } from "cs2/ui"
import { entityKey } from "cs2/utils"
import { useState } from "react"
import mod from "../../mod.json"
import { Checkbox } from "../components/Checkbox"
import { kAllTypes, TypeFilterPicker } from "../components/TypePicker"
import { kTypeLabels } from "../constants"
import css from "./index.module.scss"
import { styles } from "./styles"
import { areasVisible$, groups$ } from "./bindings"
import { markdownRenderer } from "../shared"
import { GroupCard } from "./GroupCard"

// Re-exported: modMenuButton's own tooltip needs the live group count.
export { groups$ } from "./bindings"

const filterTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Filter the list of groups by their **type**.",
            "If **All Groups** is selected, then all groups will be listed."
        ]}
    />
)

export const GroupManagementPanel = () => {
    const [filterType, setFilterType] = useState(kAllTypes)
    const groups = useValue(groups$)
    const areasVisible = useValue(areasVisible$)

    // "All Types" keeps creation order (the binding's own order); a specific
    // type filters down to just that type, still in creation order.
    const displayedGroups = filterType === kAllTypes ? groups : groups.filter((g) => g.type === filterType)

    const onFilterChange = (type: number) => {
        setFilterType(type)
        trigger(mod.id, "setOverlayFilter", type)
    }

    const onCreateGroup = () => {
        // "All Groups" (kAllTypes) has no real type to inherit, so new
        // groups created under that filter default to Generic.
        const newGroupType = filterType === kAllTypes ? 0 : filterType
        // groups.length (not displayedGroups.length) so the suggested name
        // reflects every group, regardless of the active filter.
        trigger(mod.id, "createGroup", `New Group ${groups.length + 1}`, newGroupType)
    }

    const onAreasVisibleChange = (checked: boolean) => {
        trigger(mod.id, "setAreasVisible", checked)
    }

    return (
        <>
            <div style={styles.panelHeader}>
                <div style={styles.headerRow}>
                    <div style={styles.header}>District Groups</div>
                    <div style={{ display: "flex", alignItems: "center" }}>
                        <Tooltip tooltip="Adds a new group with no member districts.">
                            <button
                                className={css.newGroupButton}
                                style={styles.newGroupButton}
                                onClick={onCreateGroup}
                            >
                                New Group
                            </button>
                        </Tooltip>
                        <TypeFilterPicker
                            value={filterType}
                            onChange={onFilterChange}
                            labels={kTypeLabels}
                            allLabel="All Groups"
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
                        <div style={styles.subtle}>No groups yet. Create one above.</div>
                    )}
                    {groups.length > 0 && displayedGroups.length === 0 && (
                        <div style={styles.subtle}>No groups match this filter.</div>
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
                    checked={areasVisible}
                    onChange={onAreasVisibleChange}
                    label="Display District areas"
                    style={styles.areasToggleRow}
                />
            </div>
        </>
    )
}

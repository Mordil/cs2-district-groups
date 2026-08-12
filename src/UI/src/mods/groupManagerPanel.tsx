import { bindValue, trigger, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownToggle, FormattedParagraphs, MarkdownRenderer, Scrollable, Tooltip } from "cs2/ui";
import { CSSProperties, useEffect, useState } from "react";
import mod from "../../mod.json";
import { UilIcon } from "mods/uilIcons";
import { GameIcon, ModIcon } from "mods/modIcons";
import css from "./groupManagerPanel.module.scss";
import selectorCss from "./selectorToggle.module.scss";

// Vanilla dropdown internals (Recolor's pattern): the item component and the
// editor theme aren't exported by cs2/ui, so pull them from the module registry.
const dropdownTheme: any = getModule("game-ui/editor/themes/editor-dropdown.module.scss", "classes");
const DropdownItem: any = getModule("game-ui/common/input/dropdown/items/dropdown-item.tsx", "DropdownItem");

interface Entity {
    index: number;
    version: number;
}

interface NamedEntity {
    entity: Entity;
    name: string;
}

interface Group {
    entity: Entity;
    name: string;
    type: number;
    members: NamedEntity[];
}

export const kTypeLabels = [
    "Generic",
    "Police",
    "Fire",
    "Healthcare",
    "Deathcare",
    "Garbage",
    "Elementary School",
    "High School",
    "College",
    "University",
    "Post",
    "Parks",
    "Welfare",
];

const kAllTypes = -1;
const kFilterLabels = ["All Groups", ...kTypeLabels];

const groups$ = bindValue<Group[]>(mod.id, "groups", []);
const districts$ = bindValue<NamedEntity[]>(mod.id, "districts", []);
const areasVisible$ = bindValue<boolean>(mod.id, "areasVisible", false);
const selectingGroup$ = bindValue<Entity>(mod.id, "selectingGroup", { index: 0, version: 0 });
const markdownRenderer = new MarkdownRenderer();

const sameEntity = (a: Entity, b: Entity) => a.index === b.index && a.version === b.version;

// Colors come from the game's own CSS custom properties (declared on :root in
// GameUI's index.css) rather than literals. That is how the game themes itself:
// the legacy interface styles reskin by overriding these same properties, e.g.
// `.style--bright-blue { --panelColorNormal: rgba(142,174,216,...) }`. Using the
// variables means the panel follows the active style - new UI or legacy, in any
// of its three themes - with no need to read useLegacyInterface at all. Same
// approach RoadBuilder takes by borrowing vanilla's SCSS modules.
const styles = {
    // Outer shell just clips the header/body blocks to the rounded corners;
    // the two children carry their own backgrounds (vanilla info-panel style:
    // dark title bar over a lighter gray body).
    panel: {
        position: "absolute",
        top: "60rem",
        left: "10rem",
        width: "490rem",
        maxHeight: "600rem",
        display: "flex",
        flexDirection: "column",
        color: "white",
        borderRadius: "6rem",
        fontSize: "14rem",
        overflow: "hidden",
        transition: "opacity .15s ease",
    } as const,
    panelHeader: {
        background: "rgba(24, 33, 51, 0.95)",
        padding: "10rem 10rem 10rem",
    } as const,
    panelBody: {
        background: "rgba(42, 56, 84, 0.88)",
        // Less right padding than the other sides: Scrollable reserves its
        // own space for the track, so the full 10rem on top of that left a
        // dead gap between the scrollbar and the panel edge.
        padding: "8rem 0rem 10rem 10rem",
        display: "flex",
        flexDirection: "column",
        flex: 1,
        minHeight: 0,
    } as const,
    listArea: {
        flex: 1,
        minHeight: 0,
    } as const,
    // Non-scrollable — sits above the group list, inside panelBody.
    areasToggleRow: {
        display: "flex",
        alignItems: "center",
        justifyContent: "flex-end",
        cursor: "pointer",
        paddingTop: "8rem",
        paddingRight: "10rem",
        paddingBottom: "8rem",
    } as const,
    divider: {
        height: "1rem",
        background: "rgba(255,255,255,0.15)",
        marginRight: "10rem",
        marginTop: "8rem",
        marginBottom: "8rem",
    } as const,
    // No internal scroll/cap: an expanded group grows to fit its full member
    // list, however long, so nothing is ever cut off. The panel's own outer
    // Scrollable handles scrolling once the whole list overflows the panel.
    memberList: {} as const,
    row: { display: "flex", alignItems: "center", margin: "3rem 0" } as const,
    smallButton: {
        background: "rgba(255,255,255,0.15)",
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "2rem 2rem",
    } as const,
    dangerButton: {
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "2rem 2rem",
    } as const,
    headerRow: {
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
    } as const,
    header: {
        color: "rgb(75, 195, 241)",
        fontSize: "16rem",
        textTransform: "uppercase",
        letterSpacing: "1rem",
    } as const,
    newGroupButton: {
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 10rem",
        marginRight: "6rem",
        fontWeight: "bold",
    } as const,
    subtle: { color: "rgba(255,255,255,0.6)" } as const,
    groupCard: {
        // A dark overlay reads as a recessed card against panelBody's
        // background whatever colour the active style gives it; a black alpha
        // wash is theme-neutral in a way a fixed tint would not be.
        background: "rgba(0,0,0,0.25)",
        borderRadius: "var(--panelRadius)",
        padding: "6rem",
        margin: "3rem 0",
    } as const,
};

const typePickerTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Change the **type** of the group.",
            "**Generic** groups can be assigned to any service building.",
            "All other types are only available to matching service buildings."
        ]}
    />
);

// The game's own Dropdown anchors its menu as a popup, so it overlays the
// panel instead of being clipped by the scroll container.
const TypePicker = (props: { value: number; onChange: (type: number) => void; style?: CSSProperties }) => (
    // key forces a full remount on every selection: closeOnSelect closing the
    // dropdown appears to desync the surrounding Tooltip's hover wiring for
    // the existing instance (same Dropdown mount-lifecycle fragility already
    // seen elsewhere this session) — a fresh mount sidesteps it entirely.
    <Tooltip key={props.value} tooltip={typePickerTooltip}>
        <Dropdown
            theme={dropdownTheme}
            content={kTypeLabels.map((label, i) => (
                <DropdownItem
                    key={i}
                    value={i}
                    className={dropdownTheme.dropdownItem}
                    selected={i === props.value}
                    closeOnSelect={true}
                    onChange={() => props.onChange(i)}
                >
                    <div>{label}</div>
                </DropdownItem>
            ))}
        >
            <DropdownToggle
                disabled={false}
                openIconComponent={<></>}
                closeIconComponent={<></>}
                className={selectorCss.selectorToggle}
                style={{
                    height: "22rem",
                    boxSizing: "border-box",
                    display: "flex",
                    alignItems: "center",
                    ...props.style,
                }}
            >
                <div>{kTypeLabels[props.value] ?? "?"}</div>
            </DropdownToggle>
        </Dropdown>
    </Tooltip>
);

const deleteGroupTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Permanently delete the group.",
            "Assigned buildings will lose their **operating districts**."
        ]}
    />
);

const filterTooltip = (
    <FormattedParagraphs
        renderer={markdownRenderer}
        text={[
            "Filter the list of groups by their **type**.",
            "If **All Groups** is selected, then all groups will be listed."
        ]}
    />
)
// -1 ("All Types") plus every real type. Distinct from TypePicker (which only
// offers real types, for assigning a group's own type).
const TypeFilterPicker = (props: { value: number; onChange: (type: number) => void }) => (
    // See TypePicker's identical key comment above — same fix, same reason.
    <Tooltip key={props.value} tooltip={filterTooltip}>
        <Dropdown
            theme={dropdownTheme}
            content={kFilterLabels.map((label, i) => {
                const value = i - 1;
                return (
                    <DropdownItem
                        key={value}
                        value={value}
                        className={dropdownTheme.dropdownItem}
                        selected={value === props.value}
                        closeOnSelect={true}
                        onChange={() => props.onChange(value)}
                    >
                        <div>{label}</div>
                    </DropdownItem>
                );
            })}
        >
            <DropdownToggle
                disabled={false}
                openIconComponent={<></>}
                closeIconComponent={<></>}
                className={selectorCss.selectorToggle}
            >
                <div style={{ display: "flex", alignItems: "center" }}>
                    <UilIcon name="FunnelFilter" size="12rem" />
                    <span style={{ marginLeft: "5rem" }}>
                        {props.value === kAllTypes ? "All Groups" : kTypeLabels[props.value] ?? "?"}
                    </span>
                </div>
            </DropdownToggle>
        </Dropdown>
    </Tooltip>
);

// The real vanilla checkbox (used for infoview color-toggle rows, e.g. the
// "Object color"/"Network color" checkboxes on the Fire & Rescue panel) —
// not exported from cs2/ui, so pulled from the module registry like the
// dropdown internals above. Path confirmed via rcav8tr/CS2Mod-BuildingUse,
// an open-source mod that adds its own infoview rows the same way.
const checkboxClasses: any = getModule(
    "game-ui/common/input/toggle/checkbox/checkbox.module.scss",
    "classes"
);

const Checkbox = (props: { checked: boolean; onChange: (checked: boolean) => void; label: string }) => (
    <div style={styles.areasToggleRow} onClick={() => props.onChange(!props.checked)}>
        <span style={{ marginRight: "6rem" }}>{props.label}</span>
        {/* Size comes from the vanilla checkbox.module.scss class, not us —
            scale transform shrinks it without needing to know/override the
            underlying pixel size. grayscale sidesteps needing to know which
            internal CSS variable drives its accent color — desaturates
            whatever that resolves to, landing on a neutral gray instead of
            the (likely theme-accent) blue it renders by default. */}
        <div
            className={`${checkboxClasses.toggle} ${props.checked ? "checked" : "unchecked"}`}
            style={{ transform: "scale(0.75)", filter: "grayscale(1)" }}
        >
            <div className={`${checkboxClasses.checkmark} ${props.checked ? "checked" : ""}`} />
        </div>
    </div>
);

const GroupCard = (props: { group: Group }) => {
    const { group } = props;
    const [expanded, setExpanded] = useState(false);
    const [nameDraft, setNameDraft] = useState(group.name);
    const [nameFocused, setNameFocused] = useState(false);
    const selectingGroup = useValue(selectingGroup$);
    const selectingThisGroup = sameEntity(selectingGroup, group.entity);

    // Stay in sync with external changes (e.g. our own rename echoing back
    // through the binding) — but never while the user is actively typing,
    // or every binding refresh would clobber in-progress edits.
    useEffect(() => {
        if (!nameFocused) {
            setNameDraft(group.name);
        }
    }, [group.name, nameFocused]);

    const commitName = () => {
        setNameFocused(false);
        const trimmed = nameDraft.trim();
        if (trimmed.length === 0) {
            setNameDraft(group.name);
        } else if (trimmed !== group.name) {
            trigger(mod.id, "renameGroup", group.entity, trimmed);
        }
    };

    return (
        <div style={styles.groupCard}>
            <div style={styles.row}>
                <button className={css.expandButton} onClick={() => setExpanded(!expanded)}>
                    <UilIcon name={expanded ? "ArrowDownThickStroke" : "ArrowRightThickStroke"} size="12rem" />
                </button>
                <input
                    className={css.nameInput}
                    style={{ marginRight: "4rem" }}
                    value={nameDraft}
                    onFocus={() => setNameFocused(true)}
                    onChange={(e) => setNameDraft((e.target as HTMLInputElement).value)}
                    onBlur={commitName}
                    onKeyDown={(e) => {
                        if (e.key === "Enter") {
                            (e.target as HTMLInputElement).blur();
                        }
                    }}
                />
                <TypePicker
                    value={group.type}
                    onChange={(t) => trigger(mod.id, "setGroupType", group.entity, t)}
                    style={{ marginRight: "4rem" }}
                />
                <Tooltip tooltip={deleteGroupTooltip}>
                    <button
                        className={`${css.headerDeleteButton} ${css.dangerButton}`}
                        style={styles.dangerButton}
                        onClick={() => trigger(mod.id, "deleteGroup", group.entity)}
                    >
                        <UilIcon name="Trash" size="20rem"/>
                    </button>
                </Tooltip>
            </div>

            {expanded && (
                <>
                    <div style={styles.memberList}>
                        {group.members.map((member) => (
                            <div style={styles.row} key={`${member.entity.index}:${member.entity.version}`}>
                                <div style={{ flex: 1, paddingLeft: "40rem" }}>{member.name}</div>
                                <Tooltip tooltip="Remove the district from the group.">
                                    <button
                                        className={css.memberDeleteButton}
                                        onClick={() => trigger(mod.id, "removeMember", group.entity, member.entity)}
                                    >
                                        <UilIcon name="Trash" size="20rem" />
                                    </button>
                                </Tooltip>
                            </div>
                        ))}
                    </div>

                    <button
                        className={`${css.selectDistrictsButton} ${selectingThisGroup ? css.selectDistrictsButtonActive : ""}`}
                        onClick={() => trigger(mod.id, "toggleDistrictSelection", group.entity)}
                    >
                        <GameIcon name="Districts" size="16rem" />
                        <span style={{ marginLeft: "6rem" }}>Select Districts</span>
                    </button>
                </>
            )}
        </div>
    );
};

// Matches styles.panel's transition duration below.
const kFadeDurationMs = 150;

export const GroupManager = () => {
    const [open, setOpen] = useState(false);
    // The panel wrapper stays permanently mounted for the fade transition, but
    // its interactive content (Dropdown/Tooltip in particular) is unmounted
    // after the fade-out completes and freshly remounted on every open. This
    // avoids Dropdown/Tooltip ever initializing their hover wiring while an
    // ancestor is opacity:0/pointer-events:none — the state that broke the
    // filter's tooltip when the content stayed mounted through the very
    // first (hidden) render.
    const [contentMounted, setContentMounted] = useState(false);
    const [filterType, setFilterType] = useState(kAllTypes);
    // Increments with every "New Group" click this session, regardless of
    // deletions, so the Nth click always suggests "New Group N".
    const [nextGroupNumber, setNextGroupNumber] = useState(1);
    const groups = useValue(groups$);
    const areasVisible = useValue(areasVisible$);

    // "All Types" keeps creation order (the binding's own order); a specific
    // type filters down to just that type, still in creation order.
    const displayedGroups = filterType === kAllTypes ? groups : groups.filter((g) => g.type === filterType);

    const openPanel = () => {
        setOpen(true);
        trigger(mod.id, "setOverlay", true);
        setContentMounted(true);
    };

    const closePanel = () => {
        setOpen(false);
        trigger(mod.id, "setOverlay", false);
        window.setTimeout(() => setContentMounted(false), kFadeDurationMs);
    };

    const togglePanel = () => (open ? closePanel() : openPanel());

    const onFilterChange = (type: number) => {
        setFilterType(type);
        trigger(mod.id, "setOverlayFilter", type);
    };

    const onCreateGroup = () => {
        // "All Groups" (kAllTypes) has no real type to inherit, so new
        // groups created under that filter default to Generic.
        const newGroupType = filterType === kAllTypes ? 0 : filterType;
        trigger(mod.id, "createGroup", `New Group ${nextGroupNumber}`, newGroupType);
        setNextGroupNumber((n) => n + 1);
    };

    const onAreasVisibleChange = (checked: boolean) => {
        trigger(mod.id, "setAreasVisible", checked);
    };

    // Built per-render (not a module-level constant like the other tooltips)
    // since it needs the live group count.
    const panelToggleTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                "**DISTRICT GROUPS**",
                "Create groups of districts to assign to service buildings for self-managing of **operating districts**.",
                `Existing groups: ${groups.length}`,
            ]}
        />
    );

    return (
        <>
            <Tooltip tooltip={panelToggleTooltip}>
                <button className={css.panelToggleButton} onClick={togglePanel}>
                    <ModIcon name="DistrictGroupRing" size="28rem" />
                </button>
            </Tooltip>
            {/* The wrapper stays permanently mounted so opacity is a real CSS
                transition; the content inside mounts/unmounts around it (see
                contentMounted above) so Dropdown/Tooltip get a fresh mount
                every time the panel opens. */}
            <div
                style={{
                    ...styles.panel,
                    opacity: open ? 1 : 0,
                    pointerEvents: open ? "auto" : "none",
                }}
            >
                {contentMounted && (
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
                                    <TypeFilterPicker value={filterType} onChange={onFilterChange} />
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
                                        key={`${group.entity.index}:${group.entity.version}`}
                                        group={group}
                                    />
                                ))}
                            </Scrollable>

                            <div style={styles.divider} />
                            <Checkbox
                                checked={areasVisible}
                                onChange={onAreasVisibleChange}
                                label="Display District areas"
                            />
                        </div>
                    </>
                )}
            </div>
        </>
    );
};

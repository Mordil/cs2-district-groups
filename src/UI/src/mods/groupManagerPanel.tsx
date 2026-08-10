import { bindValue, trigger, useValue } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownToggle } from "cs2/ui";
import { useState } from "react";
import mod from "../../mod.json";
import { UilIcon } from "mods/uilIcons";
import css from "./groupManagerPanel.module.scss";

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

const groups$ = bindValue<Group[]>(mod.id, "groups", []);
const districts$ = bindValue<NamedEntity[]>(mod.id, "districts", []);

const sameEntity = (a: Entity, b: Entity) => a.index === b.index && a.version === b.version;

const styles = {
    button: {
        pointerEvents: "auto",
        background: "rgba(24, 33, 51, 0.85)",
        color: "white",
        padding: "6rem 12rem",
        borderRadius: "4rem",
        margin: "4rem",
    } as const,
    panel: {
        position: "absolute",
        top: "60rem",
        left: "10rem",
        width: "340rem",
        maxHeight: "600rem",
        display: "flex",
        flexDirection: "column",
        pointerEvents: "auto",
        background: "rgba(24, 33, 51, 0.95)",
        color: "white",
        borderRadius: "6rem",
        padding: "10rem",
        fontSize: "14rem",
    } as const,
    listArea: {
        flex: 1,
        minHeight: 0,
        overflowY: "auto",
    } as const,
    // Roughly five member rows tall; longer lists scroll internally.
    memberList: {
        maxHeight: "140rem",
        overflowY: "auto",
    } as const,
    row: { display: "flex", alignItems: "center", margin: "3rem 0" } as const,
    smallButton: {
        background: "rgba(255,255,255,0.15)",
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "2rem 2rem",
    } as const,
    dangerButton: {
        background: "rgba(160,40,40,0.8)",
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "2rem 2rem",
    } as const,
    input: {
        background: "rgba(255,255,255,0.1)",
        color: "white",
        borderRadius: "3rem",
        padding: "3rem 6rem",
        border: "1rem solid rgba(255,255,255,0.3)",
        flex: 1,
    } as const,
    header: {
        fontWeight: "bold",
        fontSize: "16rem",
        marginBottom: "6rem",
        textTransform: "uppercase",
        letterSpacing: "1rem",
    } as const,
    footerButton: {
        width: "100%",
        background: "rgba(46, 125, 50, 0.9)",
        color: "white",
        borderRadius: "4rem",
        padding: "8rem 0",
        marginTop: "8rem",
        fontWeight: "bold",
        textAlign: "center",
    } as const,
    subtle: { color: "rgba(255,255,255,0.6)" } as const,
    groupCard: {
        background: "rgba(255,255,255,0.07)",
        borderRadius: "4rem",
        padding: "6rem",
        margin: "6rem 0",
    } as const,
};

// The game's own Dropdown anchors its menu as a popup, so it overlays the
// panel instead of being clipped by the scroll container.
const TypePicker = (props: { value: number; onChange: (type: number) => void }) => (
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
        <DropdownToggle disabled={false} style={{ backgroundColor: "rgba(93, 102, 116, 0.9)" }}>
            <div>{kTypeLabels[props.value] ?? "?"}</div>
        </DropdownToggle>
    </Dropdown>
);

const GroupCard = (props: { group: Group; districts: NamedEntity[] }) => {
    const { group, districts } = props;
    const [renaming, setRenaming] = useState(false);
    const [nameDraft, setNameDraft] = useState(group.name);
    const [expanded, setExpanded] = useState(false);

    const candidates = districts.filter(
        (d) => !group.members.some((m) => sameEntity(m.entity, d.entity))
    );

    return (
        <div style={styles.groupCard}>
            <div style={styles.row}>
                <button className={css.expandButton} onClick={() => setExpanded(!expanded)}>
                    <UilIcon name={expanded ? "ArrowDownThickStroke" : "ArrowRightThickStroke"} size="12rem" />
                </button>
                {renaming ? (
                    <input
                        style={styles.input}
                        value={nameDraft}
                        onChange={(e) => setNameDraft((e.target as HTMLInputElement).value)}
                    />
                ) : (
                    <div style={{ flex: 1, fontWeight: "bold" }}>{group.name}</div>
                )}
                <TypePicker value={group.type} onChange={(t) => trigger(mod.id, "setGroupType", group.entity, t)} />
                {renaming ? (
                    <button
                        style={styles.smallButton}
                        onClick={() => {
                            trigger(mod.id, "renameGroup", group.entity, nameDraft);
                            setRenaming(false);
                        }}
                    >
                        <UilIcon name="Checkmark" />
                    </button>
                ) : (
                    <button style={styles.smallButton} onClick={() => { setNameDraft(group.name); setRenaming(true); }}>
                        <UilIcon name="Pencil" />
                    </button>
                )}
                <button style={styles.dangerButton} onClick={() => trigger(mod.id, "deleteGroup", group.entity)}>
                    <UilIcon name="Trash" />
                </button>
            </div>

            {expanded && (
                <>
                    <div style={styles.memberList}>
                        {group.members.map((member) => (
                            <div style={styles.row} key={`${member.entity.index}:${member.entity.version}`}>
                                <div style={{ flex: 1, paddingLeft: "8rem" }}>{member.name}</div>
                                <button
                                    style={styles.dangerButton}
                                    onClick={() => trigger(mod.id, "removeMember", group.entity, member.entity)}
                                >
                                    <UilIcon name="Minus" height="16rem" width="8rem"/>
                                </button>
                            </div>
                        ))}
                    </div>

                    {candidates.length > 0 && (
                        <div style={{ ...styles.row, justifyContent: "flex-end" }}>
                            <Dropdown
                                theme={dropdownTheme}
                                content={candidates.map((d) => (
                                    <DropdownItem
                                        key={`${d.entity.index}:${d.entity.version}`}
                                        value={d.entity}
                                        className={dropdownTheme.dropdownItem}
                                        closeOnSelect={true}
                                        onChange={() => trigger(mod.id, "addMember", group.entity, d.entity)}
                                    >
                                        <div>{d.name}</div>
                                    </DropdownItem>
                                ))}
                            >
                                <DropdownToggle
                                    disabled={false}
                                    style={{ backgroundColor: "rgba(46, 125, 50, 0.9)" }}
                                >
                                    <div style={{ display: "flex", alignItems: "center" }}>
                                        <UilIcon name="Plus" size="12rem" />
                                        <span style={{ marginLeft: "5rem" }}>Add District</span>
                                    </div>
                                </DropdownToggle>
                            </Dropdown>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};

export const GroupManager = () => {
    const [open, setOpen] = useState(false);
    const groups = useValue(groups$);
    const districts = useValue(districts$);

    const togglePanel = () => {
        const next = !open;
        setOpen(next);
        trigger(mod.id, "setOverlay", next);
    };

    return (
        <>
            <button style={styles.button} onClick={togglePanel}>
                Districts ({groups.length})
            </button>
            {open && (
                <div style={styles.panel}>
                    <div style={styles.header}>District Groups</div>

                    <div style={styles.listArea}>
                        {groups.length === 0 && (
                            <div style={styles.subtle}>No groups yet. Create one below.</div>
                        )}
                        {groups.map((group) => (
                            <GroupCard
                                key={`${group.entity.index}:${group.entity.version}`}
                                group={group}
                                districts={districts}
                            />
                        ))}
                    </div>

                    <button
                        style={styles.footerButton}
                        onClick={() => trigger(mod.id, "createGroup", "New Group", 0)}
                    >
                        Add District
                    </button>
                </div>
            )}
        </>
    );
};

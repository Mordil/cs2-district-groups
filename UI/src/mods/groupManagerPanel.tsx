import { bindValue, trigger, useValue } from "cs2/api";
import { useState } from "react";
import mod from "../../mod.json";

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
        overflowY: "auto",
        pointerEvents: "auto",
        background: "rgba(24, 33, 51, 0.95)",
        color: "white",
        borderRadius: "6rem",
        padding: "10rem",
        fontSize: "14rem",
    } as const,
    row: { display: "flex", alignItems: "center", margin: "3rem 0" } as const,
    smallButton: {
        background: "rgba(255,255,255,0.15)",
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "0 2rem",
    } as const,
    dangerButton: {
        background: "rgba(160,40,40,0.8)",
        color: "white",
        borderRadius: "3rem",
        padding: "2rem 8rem",
        margin: "0 2rem",
    } as const,
    input: {
        background: "rgba(255,255,255,0.1)",
        color: "white",
        borderRadius: "3rem",
        padding: "3rem 6rem",
        border: "1rem solid rgba(255,255,255,0.3)",
        flex: 1,
    } as const,
    header: { fontWeight: "bold", fontSize: "16rem", marginBottom: "6rem" } as const,
    subtle: { color: "rgba(255,255,255,0.6)" } as const,
    groupCard: {
        background: "rgba(255,255,255,0.07)",
        borderRadius: "4rem",
        padding: "6rem",
        margin: "6rem 0",
    } as const,
};

const TypePicker = (props: { value: number; onChange: (type: number) => void }) => {
    const [open, setOpen] = useState(false);
    return (
        <div style={{ position: "relative" }}>
            <button style={styles.smallButton} onClick={() => setOpen(!open)}>
                {kTypeLabels[props.value] ?? "?"} ▾
            </button>
            {open && (
                <div style={{ ...styles.panel, position: "absolute", top: "100%", left: 0, width: "180rem", zIndex: 10, maxHeight: "300rem" }}>
                    {kTypeLabels.map((label, i) => (
                        <button
                            key={i}
                            style={{ ...styles.smallButton, display: "block", width: "100%", margin: "2rem 0" }}
                            onClick={() => {
                                props.onChange(i);
                                setOpen(false);
                            }}
                        >
                            {label}
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
};

const GroupCard = (props: { group: Group; districts: NamedEntity[] }) => {
    const { group, districts } = props;
    const [renaming, setRenaming] = useState(false);
    const [nameDraft, setNameDraft] = useState(group.name);
    const [addOpen, setAddOpen] = useState(false);

    const candidates = districts.filter(
        (d) => !group.members.some((m) => sameEntity(m.entity, d.entity))
    );

    return (
        <div style={styles.groupCard}>
            <div style={styles.row}>
                {renaming ? (
                    <input
                        style={styles.input}
                        value={nameDraft}
                        onChange={(e) => setNameDraft((e.target as HTMLInputElement).value)}
                    />
                ) : (
                    <div style={{ flex: 1, fontWeight: "bold" }}>{group.name}</div>
                )}
                {renaming ? (
                    <button
                        style={styles.smallButton}
                        onClick={() => {
                            trigger(mod.id, "renameGroup", group.entity, nameDraft);
                            setRenaming(false);
                        }}
                    >
                        ✓
                    </button>
                ) : (
                    <button style={styles.smallButton} onClick={() => { setNameDraft(group.name); setRenaming(true); }}>
                        ✎
                    </button>
                )}
                <TypePicker value={group.type} onChange={(t) => trigger(mod.id, "setGroupType", group.entity, t)} />
                <button style={styles.dangerButton} onClick={() => trigger(mod.id, "deleteGroup", group.entity)}>
                    ×
                </button>
            </div>

            {group.members.map((member) => (
                <div style={styles.row} key={`${member.entity.index}:${member.entity.version}`}>
                    <div style={{ flex: 1, paddingLeft: "8rem" }}>{member.name}</div>
                    <button
                        style={styles.dangerButton}
                        onClick={() => trigger(mod.id, "removeMember", group.entity, member.entity)}
                    >
                        −
                    </button>
                </div>
            ))}

            {candidates.length > 0 && (
                <div style={styles.row}>
                    <button style={styles.smallButton} onClick={() => setAddOpen(!addOpen)}>
                        + Add district ▾
                    </button>
                </div>
            )}
            {addOpen &&
                candidates.map((d) => (
                    <div style={styles.row} key={`${d.entity.index}:${d.entity.version}`}>
                        <button
                            style={{ ...styles.smallButton, flex: 1 }}
                            onClick={() => {
                                trigger(mod.id, "addMember", group.entity, d.entity);
                                setAddOpen(false);
                            }}
                        >
                            {d.name}
                        </button>
                    </div>
                ))}
        </div>
    );
};

export const GroupManager = () => {
    const [open, setOpen] = useState(false);
    const [newName, setNewName] = useState("");
    const [newType, setNewType] = useState(0);
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

                    <div style={styles.row}>
                        <input
                            style={styles.input}
                            placeholder="New group name"
                            value={newName}
                            onChange={(e) => setNewName((e.target as HTMLInputElement).value)}
                        />
                        <TypePicker value={newType} onChange={setNewType} />
                        <button
                            style={styles.smallButton}
                            onClick={() => {
                                if (newName.trim().length > 0) {
                                    trigger(mod.id, "createGroup", newName.trim(), newType);
                                    setNewName("");
                                }
                            }}
                        >
                            Create
                        </button>
                    </div>

                    {groups.length === 0 && <div style={styles.subtle}>No groups yet. Create one above.</div>}
                    {groups.map((group) => (
                        <GroupCard
                            key={`${group.entity.index}:${group.entity.version}`}
                            group={group}
                            districts={districts}
                        />
                    ))}
                </div>
            )}
        </>
    );
};

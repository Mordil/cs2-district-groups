import { trigger } from "cs2/api";
import { getModule } from "cs2/modding";
import { useState } from "react";
import mod from "../../mod.json";
import { kTypeLabels } from "mods/groupManagerPanel";

interface Entity {
    index: number;
    version: number;
}

interface GroupOption {
    entity: Entity;
    name: string;
    type: number;
}

const InfoSection: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx",
    "InfoSection"
);
const InfoRow: any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx",
    "InfoRow"
);

const buttonStyle = {
    background: "rgba(255,255,255,0.15)",
    color: "white",
    borderRadius: "3rem",
    padding: "2rem 10rem",
    margin: "1rem 2rem",
} as const;

const dangerStyle = {
    ...buttonStyle,
    background: "rgba(160,40,40,0.8)",
} as const;

const Section = (props: any) => {
    const [pickerOpen, setPickerOpen] = useState(false);
    const candidates: GroupOption[] = props.candidates ?? [];
    const hasAssignment: boolean = props.hasAssignment ?? false;
    const assignedGroupName: string = props.assignedGroupName ?? "";

    return (
        <InfoSection disableFocus={true}>
            <InfoRow
                left={"District Group"}
                right={
                    hasAssignment ? (
                        <>
                            {assignedGroupName}
                            <button style={dangerStyle} onClick={() => trigger(mod.id, "unassignGroup")}>
                                ×
                            </button>
                        </>
                    ) : (
                        "None"
                    )
                }
                uppercase={false}
                disableFocus={true}
            />
            {candidates.length > 0 && (
                <InfoRow
                    subRow={true}
                    disableFocus={true}
                    left={""}
                    right={
                        <button style={buttonStyle} onClick={() => setPickerOpen(!pickerOpen)}>
                            Assign group ▾
                        </button>
                    }
                />
            )}
            {pickerOpen &&
                candidates.map((candidate) => (
                    <InfoRow
                        key={`${candidate.entity.index}:${candidate.entity.version}`}
                        subRow={true}
                        disableFocus={true}
                        left={""}
                        right={
                            <button
                                style={buttonStyle}
                                onClick={() => {
                                    trigger(mod.id, "assignGroup", candidate.entity);
                                    setPickerOpen(false);
                                }}
                            >
                                {candidate.name} ({kTypeLabels[candidate.type] ?? "?"})
                            </button>
                        }
                    />
                ))}
        </InfoSection>
    );
};

// Middleware for the vanilla selected-info panel: the key must be the FULL C#
// type name of the InfoSectionBase system that writes this section's data.
export const DistrictGroupSectionComponent = (componentList: any): any => {
    componentList["multi_district_tool.DistrictGroupSection"] = (props: any) => <Section {...props} />;
    return componentList;
};

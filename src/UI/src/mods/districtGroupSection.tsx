import { trigger } from "cs2/api";
import { getModule } from "cs2/modding";
import { Dropdown, DropdownToggle, FormattedParagraphs, MarkdownRenderer } from "cs2/ui";
import mod from "../../mod.json";
import { kTypeLabels } from "mods/groupManagerPanel";
import selectorCss from "./selectorToggle.module.scss";

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

// Same vanilla dropdown internals used by the group manager panel.
const dropdownTheme: any = getModule("game-ui/editor/themes/editor-dropdown.module.scss", "classes");
const DropdownItem: any = getModule("game-ui/common/input/dropdown/items/dropdown-item.tsx", "DropdownItem");

const kNoneEntity: Entity = { index: 0, version: 0 };

// FormattedParagraphs + MarkdownRenderer is the actual vanilla component for
// multi-paragraph tooltip text (confirmed against a real vanilla tooltip's
// rendered DOM: <div class="paragraphs_..."><p class="p_..." cohinline="cohinline">).
// It supplies the correct classes, width, and the cohtml-specific cohinline
// attribute that hand-built <div>/<p> markup can't reproduce.
const markdownRenderer = new MarkdownRenderer();

const kGenericType = 0;

// Matching-type groups first (alphabetized), then Generic groups (alphabetized).
const sortCandidates = (candidates: GroupOption[], buildingType: number): GroupOption[] =>
    [...candidates].sort((a, b) => {
        const aMatches = a.type === buildingType;
        const bMatches = b.type === buildingType;
        if (aMatches !== bMatches) {
            return aMatches ? -1 : 1;
        }
        return a.name.localeCompare(b.name);
    });

const Section = (props: any) => {
    const buildingType: number = props.buildingType ?? kGenericType;
    const candidates: GroupOption[] = sortCandidates(props.candidates ?? [], buildingType);
    const hasAssignment: boolean = props.hasAssignment ?? false;
    const assignedGroupName: string = props.assignedGroupName ?? "";

    const sectionTooltip = (
        <FormattedParagraphs
            renderer={markdownRenderer}
            text={[
                "Service buildings can be assigned to a **district group**.",
                "When assigned, the group will manage the **operating districts** for the building.",
                "When unassigned, **operating districts** are managed manually.",
            ]}
        />
    );

    return (
        <InfoSection disableFocus={true} tooltip={sectionTooltip}>
            <InfoRow
                left={"DISTRICT GROUP"}
                right={
                    <Dropdown
                        theme={dropdownTheme}
                        content={[
                            <DropdownItem
                                key="none"
                                value={kNoneEntity}
                                className={dropdownTheme.dropdownItem}
                                selected={!hasAssignment}
                                closeOnSelect={true}
                                onChange={() => trigger(mod.id, "unassignGroup")}
                            >
                                <div>None</div>
                            </DropdownItem>,
                            ...candidates.map((candidate) => (
                                <DropdownItem
                                    key={`${candidate.entity.index}:${candidate.entity.version}`}
                                    value={candidate.entity}
                                    className={dropdownTheme.dropdownItem}
                                    closeOnSelect={true}
                                    onChange={() => trigger(mod.id, "assignGroup", candidate.entity)}
                                >
                                    <div>{`${candidate.name} (${kTypeLabels[candidate.type] ?? "?"})`}</div>
                                </DropdownItem>
                            )),
                        ]}
                    >
                        <DropdownToggle
                            disabled={false}
                            openIconComponent={<></>}
                            closeIconComponent={<></>}
                            className={selectorCss.selectorToggle}
                        >
                            <div>{hasAssignment ? assignedGroupName : "Unassigned"}</div>
                        </DropdownToggle>
                    </Dropdown>
                }
                uppercase={true}
                disableFocus={true}
            />
        </InfoSection>
    );
};

// Middleware for the vanilla selected-info panel: the key must be the FULL C#
// type name of the InfoSectionBase system that writes this section's data.
export const DistrictGroupSectionComponent = (componentList: any): any => {
    componentList["DistrictGroups.DistrictGroupSection"] = (props: any) => <Section {...props} />;
    return componentList;
};

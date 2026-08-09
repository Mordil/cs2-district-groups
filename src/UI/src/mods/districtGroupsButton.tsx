import { bindValue, trigger, useValue } from "cs2/api";
import mod from "../../mod.json";

const groupCount$ = bindValue<number>(mod.id, "groupCount");

// Phase 5.1 round-trip probe: shows a live value from C# and fires a trigger
// back into C# on click (logged in the mod log).
export const DistrictGroupsButton = () => {
    const groupCount = useValue(groupCount$);
    return (
        <button
            style={{
                pointerEvents: "auto",
                background: "rgba(0, 0, 0, 0.6)",
                color: "white",
                padding: "6rem 10rem",
                borderRadius: "4rem",
                margin: "4rem",
            }}
            onClick={() => trigger(mod.id, "test")}
        >
            Groups: {groupCount}
        </button>
    );
};

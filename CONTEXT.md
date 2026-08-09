# Session Context — things not captured in the other docs

Companion to `RESEARCH.md` (engine facts & findings), `CLAUDE_IMPL_PLAN.md` (plan & status), `BUILDING.md` (build workflow), `KNOWN_ISSUES.md` (user-facing caveats). Written 2026-08-08 at the end of the Phase 0–2 session.

## Test environment

- **Test saves:**
  - `Mod Test` — the main test save. Contains mod data: one group **"Test Group" (Garbage)** with 4 members (Pinewood Way, Shepherd Ridge, Cypress Forest, Ravenwood), assigned to the **Fire House** and the **Landfill**.
  - `Mod Test - mod disabled` — saved while the mod was uninstalled; mod data permanently stripped. Buildings still have 4-district `ServiceDistrict` buffers but **no group assignments** (orphaned-but-valid state). Useful for testing "reinstalled user" scenarios.
- **Test city:** 9 districts (Cypress Forest, Ravenwood, Pinewood Way, Shepherd Ridge, Hawthorne Croft, Rosewood Gardens, Briarwood Crossing, Coleridge Dale, Shepherd Glen). Service buildings used in testing: Landfill, Fire House, Small Medical Clinic.
- The user's Steam launch options are already set: `--developerMode --uiDeveloperMode` plus continue-last-save.

## Gotchas discovered but easy to miss

- **You cannot disable the mod by moving it to a sibling folder inside the game's directory.** A `Mods.disabled\` folder next to `Mods\` still gets discovered and loaded (verified — the game logged loading from `Mods.disabled/`). To truly disable, move the deployed folder completely outside `...\Colossal Order\Cities Skylines II\` (e.g., into the project dir). Game must be closed for the move (DLL lock).
- **Probe quirk:** `Ctrl+Shift+G` adds "the first two districts" by entity-query order, which changes between sessions — that's why Test Group accumulated 4 members over several presses. Dedupe prevents further growth. Not a bug; just don't be surprised reading dumps.
- The Phase 1 write probe (`Ctrl+Shift+W`) still exists alongside the group probes; it appends a single raw district to the selected building's buffer, bypassing groups.

## Current code map (C# in `src/Code/`, UI project in `src/UI/`)

| File | Contents |
|---|---|
| `Mod.cs` | `OnLoad`: settings + locale + keybinding registration, exposes `Mod.Settings`, registers `ProbeSystem` at `SystemUpdatePhase.ToolUpdate` |
| `Setting.cs` | Options page: 2 probe buttons + 4 rebindable hotkeys (Ctrl+Shift+D dump / W raw write / G group test / U unassign), `LocaleEN` |
| `ProbeSystem.cs` | Dev probes behind hotkeys/buttons; caches last selection (Escape clears vanilla selection); dumps districts/groups/selected-building state |
| `DistrictGroupComponents.cs` | `DistrictGroupData` (name + `GroupServiceType`), `DistrictGroupMember` buffer, `DistrictGroupAssignment` — all `ISerializable` |
| `DistrictGroupSystem.cs` | Group CRUD + expansion into `ServiceDistrict` buffers; `OnGamePreload` ghost purge; `OnGameLoadingComplete` dangling-ref prune |

## Working conventions from this session

- The user drives phase progression — finish the requested phase, report, and wait; don't start the next phase unprompted.
- Validation loop: code + build + deploy happen here; the user runs the game and says "check the logs"; verification is done by reading `multi_district_tool.Mod.log` (and `Player.log` / `SceneFlow.log` for load-time behavior).
- Acceptance bar: mod data correct in logs + vanilla UI reflecting it. Simulation behavior is assumed correct and is not verified (user decision — don't suggest it).
- The feasibility report artifact (linked in `RESEARCH.md`) reflects the pre-implementation study; repo docs carry the living state.

## Immediate next step (when the user asks)

Phase 3's remaining piece: live district-deletion handling during play — a small system reacting to districts gaining `Deleted`, pruning them from groups and re-expanding assigned buildings (mirror of vanilla `ServiceDistrictSystem` cleanup; the load-time half already exists in `DistrictGroupSystem`).

# Multi-District Tool — Implementation Plan

> **Status (2026-08-08):** Phases 0 ✅, 1 ✅, 2 ✅ — **M1 PASSED; M2 core PASSED.** Phase 2 verified in-game: groups persist in saves with entity remap; assign/unassign round-trips; exclusive expansion works; ghost-entity lifecycle bug found and fixed (`OnGamePreload` purge + `OnGameLoadingComplete` prune, both observed working in logs — see RESEARCH.md findings #7/#8). Registry API in `DistrictGroupSystem.cs`; components in `DistrictGroupComponents.cs`; test hotkeys Ctrl+Shift+G/U. Mod-removed test also passed: save loads clean without the mod (`Player.log`: "Not serializable type: multi_district_tool.*" — components skipped harmlessly), vanilla `ServiceDistrict` data survives stripping, and reinstalling recovers un-stripped saves fully. **Phase 2 closed with the full lifecycle matrix verified.**
>
> **Phase 3 ✅ (2026-08-08):** `DistrictGroupSyncSystem` (at `SystemUpdatePhase.Modification5`, `RequireForUpdate` on District+Deleted) prunes deleted districts from groups live and re-expands — verified in-game ("Sync: removed 1 deleted district(s)..."). Defense-in-depth: `ExpandToBuilding` skips dead/deleted members so dangling refs can never reach vanilla buffers. Also observed: vanilla `ServiceDistrictSystem` independently prunes orphaned (unassigned) buildings' buffers; repainted districts don't auto-join groups (by design); assignment to a newly plopped building works. **M2 complete. Next: Phase 4 (overlay rendering).**
>
> **Status (original):** Phase 0 ✅ and Phase 1 ✅ complete — **M1 go/no-go PASSED.** The write probe added districts to a Landfill's and a Small Medical Clinic's `ServiceDistrict` buffers; the vanilla panel showed them and the assignment **survived save/reload with entity IDs remapped correctly** (log evidence: `Cypress Forest` was `Entity(173566:1)` pre-reload, `Entity(197032:3)` post-reload, and the buffer followed). This also empirically confirms Step 2.3's warning: raw entity IDs are NOT stable across save/load. Probes live in `ProbeSystem.cs` (hotkeys Ctrl+Shift+D / Ctrl+Shift+W). Next: Phase 2.

Design context and engine research: see `RESEARCH.md`. This plan turns the settled "district groups" design into discrete, verifiable steps. Current state: the repo is the untouched official CS2 mod template (`Mod.cs`, `Setting.cs`, csproj wired to the `CSII_TOOLPATH` modding toolchain), C#-only — no UI module yet.

Each step has a **Done when** gate. Do the steps in order; later steps assume earlier ones.

---

## Phase 0 — Toolchain sanity & template cleanup

**Step 0.1 — Build and deploy the template as-is.**
Confirm `CSII_TOOLPATH` is set, `dotnet build` succeeds, the mod deploys to the game's mods folder (the toolchain's Mod.targets does this), and `multi_district_tool.Mod.OnLoad` appears in `Logs/` when the game starts.
*Done when:* log line visible in-game with the mod enabled.

**Step 0.2 — Fix template landmines.**
`Setting.SetDefaults()` currently throws `NotImplementedException` — implement it (set real defaults). Strip the demo options (Button/Slider/Dropdown groups) down to an empty-but-valid Setting + LocaleEN.
*Done when:* options screen shows the mod with no errors in the log.

**Step 0.3 — Decompile reference setup.**
Point the IDE at the game's `Game.dll` (ILSpy/dnSpy or source-link via the toolchain) so `Game.Areas.*`, `Game.UI.InGame.*`, and `Game.Rendering.*` internals are browsable locally. The ps1ke guide is the map, but exact signatures should come from the shipped assembly (the guide may lag game patches).
*Done when:* you can open `CurrentDistrictSystem` and `AreaUtils.CheckServiceDistrict` in the decompiler.

---

## Phase 1 — Probe: prove the `ServiceDistrict` buffer write

**Step 1.1 — Dev-trigger scaffold.**
Add a `GameSystemBase` (registered in `Mod.OnLoad` via `updateSystem.UpdateAt<...>`) with a crude trigger — a settings button or hotkey — that runs experiment code on demand.
*Done when:* trigger fires and logs from inside a running city.

**Step 1.2 — Read probe.**
On trigger: query all district entities (`Game.Areas.District`), log their names (via `Game.UI` name system or `Colossal` naming APIs) and entity IDs; pick a selected service building and log its current `ServiceDistrict` buffer.
*Done when:* log output matches what the vanilla building panel shows.

**Step 1.3 — Write probe (the go/no-go gate for the whole project).**
On trigger: add a district to a service building's `ServiceDistrict` buffer in code. Verify (a) the vanilla building panel shows the district as served, (b) simulation respects it (e.g., a school starts accepting students from that district / dispatch honors it), (c) it survives save + load.
*Done when:* all three checks pass. **If this fails, stop and re-evaluate the design.**

---

## Phase 2 — Domain model: groups + persistence

**Step 2.1 — Group model.**
`DistrictGroup { name, serviceType, members: list of district entities }`. Service type as an enum aligned with vanilla service categories (police, fire, education tiers, healthcare, garbage, deathcare, post, …).

**Step 2.2 — Registry system.**
A managed system owning the group list with CRUD operations: create/rename/delete group, add/remove member district, assign/unassign group ↔ service building. Assignment records which buildings use which group (needed for re-expansion later).

**Step 2.3 — Persistence via ECS serialization — not the settings file.**
Entity IDs are **not stable across save/load**; a JSON settings file holding raw entity indices will corrupt on load. Store groups in save data using the game's serialization: a mod-created singleton entity carrying custom `ISerializable` buffer components (entity references then go through the game's reader/writer remapping), following the established pattern from open-source CS2 mods that persist entity references. Mod-removed safety: everything written into `ServiceDistrict` buffers is vanilla data and keeps working; only the group definitions disappear.
*Done when:* groups (including member entity refs and building assignments) survive save/load; loading the save without the mod works fine.

**Step 2.4 — Group ⇄ buffer expansion.**
Assigning a group to a building = write all member districts into its `ServiceDistrict` buffer (merged with any hand-set districts, or exclusive — pick exclusive-per-mod-assignment for v1, simpler mental model). Unassigning removes them.
*Done when:* assign/unassign round-trips cleanly and the vanilla panel reflects it.

---

## Phase 3 — Sync: keep groups true as the map changes

**Step 3.1 — District deletion.**
Query districts gaining `Deleted` (mirror `ServiceDistrictSystem`'s own cleanup): remove them from all groups; re-expand affected groups into their buildings' buffers.

**Step 3.2 — Group edits propagate.**
Any group CRUD re-expands into every assigned building automatically — this is the mod's core value over vanilla hand-ticking.

**Step 3.3 — Edge cases.**
New base district painted inside a group's area (v1: manual add only — no geometric inference, since groups are sets, not shapes); building demolished (assignments are per-entity and die with it; registry prunes dangling building refs); buffer edited by hand in vanilla UI while a group is assigned (v1: last writer wins; log it).
*Done when:* delete-district and edit-group scenarios show correct buffers with no dangling entities, verified in a test city.

---

## Phase 4 — Visualization: colored overlays

**Step 4.1 — Single-district overlay.**
Render one district's polygon (its `Game.Areas.Node`/`Triangle` buffers) as a colored fill + outline through `Game.Rendering.OverlayRenderSystem` — the proven tool-mod pattern (Move It, Line Tool). Show it only while the mod's UI is active.

**Step 4.2 — Per-group coloring.**
Color assignment per group (auto-palette + user override later). Active-type view: selecting "police" tints every police group's member districts in its group color; overlap of a district in two same-type groups uses the selected group's color on top.

**Step 4.3 — (Optional experiment) persistent colors in the vanilla district view.**
Try attaching `Game.Objects.Color` to a district entity — `AreaColorSystem`'s fill job reads that lookup, so districts *may* respect per-entity color while an infoview is active. Timebox this; it's a bonus, not a dependency.
*Done when:* opening the mod panel shows group-colored districts; performance is fine on a large city.

---

## Phase 5 — UI

**Step 5.1 — UI module scaffold.**
Add the cohtml/React UI side (the official mod template's "with UI" variant: Node toolchain, `UI/` folder, hot-reload against the game). Wire a trivial button → C# binding round-trip.
*Done when:* clicking a mod button in-game logs in C#.

**Step 5.2 — Group manager panel.**
List groups by type; create/rename/delete; edit membership by clicking base districts on the map (reuse the probe's district query + a picker tool that raycasts districts — see `Game.Tools` raycasting used by `AreaToolSystem`).

**Step 5.3 — Typed picker on service buildings.**
Investigate the vanilla selected-building panel's districts section in `Game.UI.InGame` (managed bindings — patchable/extendable, unlike Burst): either inject a "Assign district group" section (preferred; mods commonly add info-panel sections) or filter the vanilla list. Show only groups whose type matches the building's service.
*Done when:* the full loop works in UI only: paint base districts → create "Precinct 3" (police) → select police station → picker shows only police groups → assign → vanilla panel shows the member districts → dispatch respects them.

---

## Phase 6 — Validation, polish, release

**Step 6.1 — Coverage validation.** Per service type: list base districts not in any group of that type; surface in the panel and optionally tint them in the overlay.

**Step 6.2 — (Optional, post-v1) group-level policies.** Apply a policy to all member districts; warn when two groups set the same policy on a shared district.

**Step 6.3 — Hardening.** Localization pass (LocaleEN), options (default colors, overlay opacity), log hygiene, big-city perf check, save/load matrix (new city / existing save / save without mod / mod removed).

**Step 6.4 — Publish.** Fill `Properties/PublishConfiguration.xml`, screenshots of overlay + picker, PDX Mods upload; README states the honest scope: management layer over the vanilla service-districts feature, no simulation changes.

---

## Standing constraints (from RESEARCH.md — do not violate)

- Never rely on patching Burst jobs (`CurrentDistrictSystem`, simulation checks) — Harmony can't touch them. All mod writes go through managed code and vanilla data (`ServiceDistrict` buffers).
- Base districts stay vanilla and non-overlapping; groups are sets, never geometry.
- Persist entity references only through the game's serialization (remapping), never raw IDs in JSON.
- Read ps1ke guide pages via `raw.githubusercontent.com/ps1ke/Cities-Skylines-2-Modding-Guide/master/<path>.md` — the rendered site defeats fetch tools.

## Milestone summary

| Milestone | Proves |
|---|---|
| M1 (Phase 0–1) | Toolchain works; `ServiceDistrict` write is viable — **go/no-go** |
| M2 (Phase 2–3) | Groups exist, persist, and stay correct as the map changes |
| M3 (Phase 4) | Groups are visible on the map in distinct colors |
| M4 (Phase 5) | Full workflow usable entirely from in-game UI |
| M5 (Phase 6) | Published on PDX Mods |

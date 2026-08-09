# Multi-District Tool — Research Summary & Restart Point

*Last updated: 2026-08-08. Full report artifact: <https://claude.ai/code/artifact/67646a17-97b6-4587-99a8-dd7711499267>*

## What this project is

A Cities: Skylines II **management/UI mod** for organizing city services by district. Original idea was overlapping district layers (school zones, police precincts) with buildings belonging to multiple districts; after feasibility research the scope was deliberately narrowed to a **management tool with no new simulation mechanics**.

## The settled design: District Groups

- Vanilla painted districts remain the only geometry ("base districts" — small, neighborhood-sized, non-overlapping, exactly as the game already works).
- A **group** is mod-owned data: `name → type (school/police/fire/…) → list of base-district entities`. One base district can belong to many groups — this is where "overlap" lives, without any overlapping polygons.
- On a service building, a **typed picker** shows only groups matching that service type. Selecting a group expands its members into the building's vanilla `ServiceDistrict` buffer. The base game then does all simulation (dispatch, school seats, coverage) untouched.
- **Sync system:** when a group changes or a member district is deleted/split, re-expand into every assigned building's buffer.
- **Visualization:** per-group colored map overlays + validation ("these districts are in no fire group").
- Optional later: group-level policies (apply to each member district; warn on collisions).

### Why this design (decisions already made — don't re-litigate)

1. **Overlapping polygon layers — rejected.** `CurrentDistrict` on a building holds a *single* `Entity m_District` (baked into the save format). Every consumer (policies, service checks, info panel) reads that one slot, and the consumers are Burst-compiled jobs — **Harmony cannot patch Burst jobs**; you'd have to disable and reimplement whole systems.
2. **Typed painted districts with a filtered picker — rejected.** If typed polygons overlap geometrically, a building in the overlap gets one arbitrary `CurrentDistrict`, so the vanilla service match (`building's CurrentDistrict ∈ service building's ServiceDistrict list`, via `AreaUtils.CheckServiceDistrict`) silently fails for the other type.
3. **Groups over non-overlapping base districts — chosen.** Every building keeps one well-defined `CurrentDistrict`; simulation output is identical to hand-ticking districts in vanilla. The mod's value is authoring/maintenance/visualization (define a precinct once, reuse across buildings, auto-sync on map changes) — same spirit as CS1's Enhanced District Services.
4. **Scope decision (user):** management tool, not simulation mod.

## Key engine facts (from decompiled Game.dll docs)

Namespace `Game.Areas` unless noted:

| Type | Kind | Notes |
|---|---|---|
| `District` | IComponentData on district entities | Just `uint m_OptionMask`. Polygon lives in shared Area machinery (`Node`/`Triangle` buffers, `Area`, `Geometry`) — a district IS an Area entity. |
| `CurrentDistrict` | IComponentData on buildings/objects | **Single** `Entity m_District`. Maintained by `CurrentDistrictSystem` (Burst jobs, point-in-polygon vs. area search tree, one winner, behind `ModificationBarrier5`). |
| `BorderDistrict` | IComponentData on road edges | `m_Left`/`m_Right`. |
| `ServiceDistrict` | **IBufferElementData** on service buildings | The vanilla "districts served" list — **the mod's write target**. Empty = serves whole city. Checked in simulation via `AreaUtils.CheckServiceDistrict(...)`. `ServiceDistrictSystem` cleans dangling entries when a district is deleted. |
| `DistrictModifier` / `DistrictModifierType` | Buffer on district entities | 12 policy effect types (crime, fire response, parking fees, speed limits, …). |

Rendering (namespace `Game.Rendering` / `Game.Prefabs`):

- Colors are **per-prefab**, not per-district: `Game.Prefabs.AreaColorData { m_FillColor, m_EdgeColor, m_SelectionFillColor, m_SelectionEdgeColor }` on the single shared District prefab — why all districts look identical. `AreaTypePrefab` holds the materials.
- Pipeline: `AreaBatchSystem` (GPU triangle batches) → `AreaColorSystem` (per-batch colors, **only while an infoview/tool is active**) → `AreaBorderRenderSystem` (outlines).
- `AreaColorSystem`'s fill job reads a `Game.Objects.Color` component lookup → a per-entity color path may exist (unverified for districts; quick in-game experiment needed).

### Coloring routes (for distinct group colors)

1. **Mod-drawn overlay via `OverlayRenderSystem`** — recommended; proven pattern (Move It, Line Tool). District `Node`/`Triangle` buffers give exact polygons; full control, shown whenever the mod panel is open. |
2. Cloned district prefabs per type (each with own `AreaColorData`) — vanilla-integrated but per-prefab color; needs regression testing against single-prefab assumptions.
3. Per-entity `Game.Objects.Color` component — most elegant if districts respect it; **unverified**, infoview-gated.

## Prototype sequence (next steps)

1. **Buffer probe:** minimal mod writes a district into a service building's `ServiceDistrict` buffer; confirm vanilla UI + dispatch reflect it.
2. Group registry (in-memory) + expansion; verify multi-district service works; then persistence.
3. Overlay rendering of one district polygon in a custom color; run the `Objects.Color` experiment.
4. UI: group manager panel + typed picker (cohtml/React UI modding; managed C# bindings — filterable/patchable, unlike Burst).
5. Sync & safety: district delete/repaint handling, mod-removed behavior (written `ServiceDistrict` buffers are vanilla data — degrade gracefully), save/load.

## Sources

- ps1ke's CS2 Modding Guide (decompiled Game.dll reference): <https://ps1ke.github.io/Cities-Skylines-2-Modding-Guide/>
  - **Tip:** the rendered site pages serve a huge nav sidebar that defeats fetch tools; read raw markdown instead at `https://raw.githubusercontent.com/ps1ke/Cities-Skylines-2-Modding-Guide/master/<path>.md` (branch is `master`), e.g. `Game/Areas/CurrentDistrictSystem.md`, `Game/Rendering/AreaColorSystem.md`, `Game/Prefabs/AreaColorData.md`.
- Vanilla service-district feature: <https://www.paradoxinteractive.com/games/cities-skylines-ii/features/city-services-districts-policies>, <https://www.gameskinny.com/tips/cities-skylines-2-how-to-make-districts/>
- Prior art: Recolor by yenyang (per-entity recoloring) <https://www.nexusmods.com/citiesskylines2/mods/136>; CS1 Enhanced District Services (concept ancestor).

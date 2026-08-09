# District Groups

A Cities: Skylines II management mod: **reusable groups of districts for city services.**

Vanilla lets you restrict a service building to specific districts, but you tick those districts one building at a time and nothing keeps the lists in sync afterwards. District Groups lets you define a named, typed set of districts once — a fire coverage area, a school catchment, a garbage route — assign it to as many service buildings as you like, and have every assigned building's "districts served" list update automatically whenever the group or the underlying districts change.

The mod adds no new simulation mechanics. Groups expand into the game's own `ServiceDistrict` data, so dispatch, coverage, and school seats are handled entirely by the base game.

- **Groups** — `name → service type → list of districts`. One district can belong to any number of groups.
- **Typed picker** — on a service building, only groups matching its service type are offered.
- **Automatic sync** — edit a group, or delete/repaint a district, and every assigned building is re-expanded.
- **Overlay** — member districts are outlined per group in distinct colors while the mod panel is open.

Documentation: [`BUILDING.md`](BUILDING.md) (local build & deploy), [`RESEARCH.md`](RESEARCH.md) (engine findings and why the design is what it is), [`CLAUDE_IMPL_PLAN.md`](CLAUDE_IMPL_PLAN.md) (phase plan & status), [`KNOWN_ISSUES.md`](KNOWN_ISSUES.md) (user-facing limitations).

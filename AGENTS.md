# Agent Instructions

## Logging

Every log call — C# (`Mod.log.Info/Warn/Error/Debug`) and TypeScript (`logger.info/warn/error/debug`) alike — follows this shape:

```
<static message>[; <label>:<value> <label>:<value> ...]
```

- `<static message>` is fixed text, not built from runtime-interpolated values.
- The `; ` separator and the dynamic-values list are only present when there's at least one dynamic value to log. A message with nothing dynamic to report needs no trailing semicolon.
- When dynamic values are present, they're `<label>:<value>` pairs, **space-separated** (never comma-separated), with `<label>` in **snake_case** — including in TypeScript call sites.

**Good, no dynamic values:**
```ts
logger.info("Panel opened;")
```
(a trailing `;` with nothing after it is fine — it's just not required)

**Good, with dynamic values:**
```csharp
Mod.log.Info($"Registered icon host; host:{kIconHost} path:{iconDir} exists:{Directory.Exists(iconDir)}")
```

**Bad — comma-separated, `=` instead of `:`, no semicolon before dynamic content:**
```ts
logger.info(`Filter changed, type=${type}`)
```

**Bad — camelCase label:**
```ts
logger.info(`Group renamed; groupId:${id}`)   // should be group_id:${id}
```

### Exceptions

- **Multi-line human-readable diagnostic dumps** (e.g. the debug-data dump in `DistrictGroupSystem.Debug.cs`, meant to be pasted verbatim into a bug report) are not per-line event logs and are exempt from this format.
- **Fixed source-tag prefixes** — e.g. `DistrictGroupsUISystem.API.cs` prepends `"[UI] "` to relayed TypeScript log lines before forwarding them to the C# logger. That tag isn't a dynamic value and doesn't break conformance of the message it wraps.

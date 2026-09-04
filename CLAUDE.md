# District Groups — Project Notes

## JavaScript and TypeScript imports

These should be grouped by classification, and then alphabetical in the classification.

There should be a single empty newline between each group of import statements.

```
<react imports>

<cs2 imports>

<project shared imports, such as constants, utils, etc>

<sibling directory imports such as style sheets>
```

## Comments

Use the following comment styles

```
// I am an API description
```

The `//` comments are reserved for documenting code symbols like methods, properties, etc. They can be multiline, with `//` for each line.
These should ideally be no more than 1 sentence long.


```
/*
    I am historical context that is extremely evergreen.
    I explain things that are external that I do not control, like 3rd party quirks or bugs.
*/
```

The `/* */` C-style block comments are reserved for greater context that should be evergreen without things such as dependencies changing.
These should exist at the top of lexical blocks that explains several lines of code, as their WHY.

### Rules

1. After the end of each coding iteration, ask for a review of comments.
2. Do not include overly technical details, such as "mirrors the FooSystem"
3. Do not include development iteration details "This previously used to do... but we changed it when doing..."
4. All `public` API should have at least a single line of `//` documentation.

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


## Localization: every new display string needs every language

Whenever you add or change user-facing display text (a new UI string, a settings label/description, a tooltip, a dialog message, etc.), you must add a translation for **every** supported locale, not just English:

- `src/Code/Localization/LocaleEN.cs` (source of truth)
- `LocaleDE.cs`, `LocaleES.cs`, `LocaleFR.cs`, `LocaleIT.cs`, `LocaleJA.cs`, `LocaleKO.cs`, `LocalePL.cs`, `LocalePTBR.cs`, `LocaleRU.cs`, `LocaleZHHANS.cs`, `LocaleZHHANT.cs`

Adding an English-only string (or leaving a `TODO` for translations) is not an acceptable end state — every one of the files above needs the new key before the change is done.

Conventions to follow:
- Custom mod UI strings use the shared constants in `LocalizationKey.cs` (`"DistrictGroups.UI[Key]"`) — add a new constant there rather than a raw string literal, and make sure the key matches `src/UI/src/utils/locale.ts`'s `kLocale`/`kFallback` exactly.
- Settings-panel strings (tab/group names, option labels/descriptions/warnings) use `m_Setting.GetOption...LocaleID(...)` — the same key expression must appear in every locale file, so copy it verbatim across files and only change the translated value.
- Preserve `{PLACEHOLDER}` tokens, `**bold**` markers, and `\n`/`\r\n` breaks exactly across all languages.
- For service-category terms (Police, Fire, Healthcare, Deathcare, Garbage, education tiers, Post, Parks, Welfare, etc.), prefer matching the terminology the base game itself ships over inventing a translation or guessing from web search. The shipped strings can be extracted directly: `Cities2_Data/Content/Game/Locale.cok` is a zip archive (readable with Python's `zipfile`) containing one `<locale>.loc` file per language. Each `.loc` file is: little-endian int16 version, three `.NET BinaryWriter`-style length-prefixed header strings, a little-endian int32 entry count, then that many key/value pairs (each a 7-bit-encoded length prefix + UTF-8 bytes) — grep the decoded English values for the term you're translating to find the right key, then read the same key from the target language's `.loc`.

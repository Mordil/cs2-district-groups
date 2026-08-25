# District Groups — Project Notes

## Localization: every new display string needs every language

Whenever you add or change user-facing display text (a new UI string, a settings label/description, a tooltip, a dialog message, etc.), you must add a translation for **every** supported locale, not just English:

- `src/Code/Localization/LocaleEN.cs` (source of truth)
- `LocaleDE.cs`, `LocaleES.cs`, `LocaleFR.cs`, `LocaleIT.cs`, `LocaleJA.cs`, `LocaleKO.cs`, `LocalePL.cs`, `LocalePTBR.cs`, `LocaleRU.cs`, `LocaleZHHANS.cs`, `LocaleZHHANT.cs`

Adding an English-only string (or leaving a `TODO` for translations) is not an acceptable end state — every one of the files above needs the new key before the change is done.

Conventions to follow:
- Custom mod UI strings use the shared constants in `LocalizationKey.cs` (`"DistrictGroups.UI[Key]"`) — add a new constant there rather than a raw string literal, and make sure the key matches `src/UI/src/locale.ts`'s `kLocale`/`kFallback` exactly.
- Settings-panel strings (tab/group names, option labels/descriptions/warnings) use `m_Setting.GetOption...LocaleID(...)` — the same key expression must appear in every locale file, so copy it verbatim across files and only change the translated value.
- Preserve `{PLACEHOLDER}` tokens, `**bold**` markers, and `\n`/`\r\n` breaks exactly across all languages.
- For service-category terms (Police, Fire, Healthcare, Deathcare, Garbage, education tiers, Post, Parks, Welfare, etc.), prefer matching the terminology the base game itself ships over inventing a translation or guessing from web search. The shipped strings can be extracted directly: `Cities2_Data/Content/Game/Locale.cok` is a zip archive (readable with Python's `zipfile`) containing one `<locale>.loc` file per language. Each `.loc` file is: little-endian int16 version, three `.NET BinaryWriter`-style length-prefixed header strings, a little-endian int32 entry count, then that many key/value pairs (each a 7-bit-encoded length prefix + UTF-8 bytes) — grep the decoded English values for the term you're translating to find the right key, then read the same key from the target language's `.loc`.

# Known Issues & Limitations

User-facing behaviors to document on the mod page / README.

## Uninstalling the mod: group data is lost on the first save without it

District **groups** (names, types, member lists, building assignments) are stored inside your save file using the game's own mod-data mechanism. What happens without the mod installed:

- **Loading a save without the mod is safe and fully reversible.** The game logs that it skipped the mod's data (`Not serializable type: DistrictGroups.*` in `Player.log`) and everything else loads normally. If you reinstall the mod and load that same save file again, all groups come back intact.
- **Saving without the mod permanently strips the group data from that save file.** The skipped data no longer exists in memory, so it can't be written back. Reinstalling the mod later will show no groups in that save. There is no recovery.
- **Service assignments themselves always survive.** The districts a building serves are written into the game's own vanilla data (the same "districts served" list you can edit by hand), so buildings keep serving their assigned districts even with the mod gone forever — only the group definitions (the reusable named sets) are lost.

**User guidance:** if you want to try removing the mod, keep a backup save from before the removal — or simply don't save while the mod is uninstalled.

## Group assignment is exclusive (v1)

While a building is assigned to a group, the mod owns that building's entire "districts served" list — hand-edits in the vanilla panel will be overwritten the next time the group re-expands (on group edit or on save load). Unassigning the group clears the list back to "serves the whole city".

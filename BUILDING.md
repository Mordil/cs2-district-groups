# Building & Updating the Mod Locally

How to rebuild and redeploy this mod from this machine. Verified working 2026-08-08 (Phase 0 of `CLAUDE_IMPL_PLAN.md`).

## Environment facts (this machine)

| Thing | Value |
|---|---|
| Dev shell | WSL2 (Linux), project on the Windows filesystem |
| Project dir (WSL) | `/mnt/c/Users/Nathan/Documents/Unity Projects/multi-district-tool` |
| CS2 modding toolchain (`CSII_TOOLPATH`) | `C:\Users\Nathan\AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Modding` |
| Game managed assemblies (`CSII_MANAGEDPATH`) | `G:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\Managed` (contains `Game.dll`) |
| Deploy target (automatic) | `C:\Users\Nathan\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\multi-district-tool` |
| Game logs | `C:\Users\Nathan\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\` |

`CSII_TOOLPATH` and `CSII_MANAGEDPATH` are **Windows user environment variables**. Linux processes in WSL do not see them, and the csproj/Mod.props read them via `EnvironmentVariableTarget.User` — so the build must run through a **Windows** process.

## Build (the one command that matters)

From WSL, in the project directory:

```bash
cd "/mnt/c/Users/Nathan/Documents/Unity Projects/multi-district-tool" && cmd.exe /c "dotnet build"
```

- Do **not** use a Linux `dotnet` (it can't resolve the toolchain env vars). `cmd.exe /c "dotnet build"` uses Windows dotnet (`C:\Program Files\dotnet\dotnet.exe`) with the user env vars present.
- A successful build takes ~10–60 s (first build runs Unity IL post-processing + Burst compilation for win/mac/linux) and ends with:
  ```
  Copy output to deploy directory C:\Users\Nathan\AppData\LocalLow\...\Mods\multi-district-tool
  Build succeeded.
  ```
- Release build: `cmd.exe /c "dotnet build -c Release"`.

## Deploy & reload rules

- **Deployment is automatic** — the toolchain's `Mod.targets` copies the built mod into the game's `Mods` folder on every successful build. There is no separate install step.
- **Close the game before rebuilding.** While CS2 is running it holds the deployed DLL open; the copy step (and thus the build) will fail.
- **No hot reload.** Local mods load once at game startup. After a rebuild, restart the game.
- Local mods in the `Mods` folder are **auto-enabled** — nothing to toggle in-game.

## Verifying a deployed build

1. Start CS2, reach the main menu (or load a city).
2. Check the mod's own log (logger is named `multi_district_tool.Mod`):
   ```bash
   tail "/mnt/c/Users/Nathan/AppData/LocalLow/Colossal Order/Cities Skylines II/Logs/multi_district_tool.Mod.log"
   ```
   A healthy load shows `OnLoad` and the mod asset path.
3. Cross-check `Logs/Modding.log` for the assembly + Burst library load lines:
   ```
   Loaded additional Burst code ...multi-district-tool_win_x86_64.dll
   Loaded multi-district-tool, Version=1.0.0.0 ...
   ```
4. The options screen should list "Multi-District Tool".

## Iteration tips (no C# hot reload exists)

Mod assemblies are net48/Mono — they load once at game startup and cannot be unloaded. Every C# change requires: close game → build → relaunch. To make that loop cheaper:

- **Steam launch options:** `--developerMode --uiDeveloperMode`
  - `--developerMode`: Tab/Home opens the in-game developer UI. Its **Scene Explorer inspects live entities and components** — e.g., check a building's `ServiceDistrict` buffer or a district's components with zero code/rebuild.
  - `--uiDeveloperMode`: live-reloads mod UI (cohtml/React) on change — the Phase 5 UI work iterates without game restarts; only C# binding changes need a restart.
- **Batch experiments per restart:** keep probe/experiment code behind runtime triggers (settings buttons, hotkeys) and parameterize them, so one build answers several questions.
- **Dedicated test save:** a minimal city with a few painted districts and one police station/school as the first save in the load menu.

## Troubleshooting

- **Build fails at "Copy output to deploy directory"** → the game is running; close it and rebuild.
- **`Mod.props` not found / import errors** → `CSII_TOOLPATH` missing or the game hasn't regenerated the toolchain; launch CS2 once (it writes `.cache\Modding`), and confirm with `cmd.exe /c "echo %CSII_TOOLPATH%"`.
- **`Game.dll` reference errors** → check `CSII_MANAGEDPATH` (`cmd.exe /c "echo %CSII_MANAGEDPATH%"`); the game may have moved between Steam libraries.
- **Mod loads but changes absent in game** → you launched the game before the rebuild finished, or the build actually failed — re-check the build output, then restart the game.

## Notes for future work

- The toolchain Burst-compiles the mod assembly (win/mac/linux) — our own jobs *can* use Burst. The design constraint from `RESEARCH.md` is only that vanilla Burst jobs can't be Harmony-patched.
- Decompiler access: the csproj references resolve against `CSII_MANAGEDPATH`, so in Rider, Ctrl+Click any `Game.*` type to decompile the shipped assembly (authoritative over the ps1ke guide, which may lag patches).
- `ProbeSystem.cs` exists in the repo but is **not registered** in `Mod.OnLoad` — it's dormant Phase 1 scaffolding and compiles into the DLL without running.

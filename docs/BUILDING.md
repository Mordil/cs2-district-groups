# Building & Updating the Mod Locally

How to rebuild and redeploy this mod.

## Common Paths

| Thing | Value |
|---|---|
| CS2 modding toolchain (`CSII_TOOLPATH`) | `C:{USER}\AppData\LocalLow\Colossal Order\Cities Skylines II\.cache\Modding` |
| Game managed assemblies (`CSII_MANAGEDPATH`) | `G:{STEAM_PATH}\common\Cities Skylines II\Cities2_Data\Managed` (contains `Game.dll`) |
| Deploy target (automatic) | `C:{USER}\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\district-groups` |
| Game logs | `C:{USER}\AppData\LocalLow\Colossal Order\Cities Skylines II\Logs\` |

## Build
```bash
cmd.exe /c "dotnet build"
```

- Do **not** use a Linux `dotnet` (it can't resolve the toolchain env vars). `cmd.exe /c "dotnet build"` uses Windows dotnet (`C:\Program Files\dotnet\dotnet.exe`) with the user env vars present.
- A successful build takes ~10–60 s (first build runs Unity IL post-processing + Burst compilation for win/mac/linux)
- Release build: `cmd.exe /c "dotnet build -c Release"`, or `./build -r` (add `-n` to skip deploying).

## Deploy & reload rules

- **Deployment is automatic** — the toolchain's `Mod.targets` copies the built mod into the game's `Mods` folder on every successful build. There is no separate install step.
- **Close the game before rebuilding.** While CS2 is running it holds the deployed DLL open; the copy step (and thus the build) will fail.
- **No hot reload.** Local mods load once at game startup. After a rebuild, restart the game.
- Local mods in the `Mods` folder are **auto-enabled** — nothing to toggle in-game.

## Iteration tips

- **Steam launch options:** `--developerMode --uiDeveloperMode`
  - `--developerMode`: Tab/Home opens the in-game developer UI. Its **Scene Explorer inspects live entities and components** — e.g., check a building's `ServiceDistrict` buffer or a district's components with zero code/rebuild.
  - `--uiDeveloperMode`: live-reloads mod UI (cohtml/React) on change — only C# binding changes need a restart
- **Batch experiments per restart:** keep probe/experiment code behind runtime triggers (settings buttons, hotkeys) and parameterize them, so one build answers several questions.
- **Dedicated test save:** a minimal city with a few painted districts and one police station/school as the first save in the load menu.

## UI module

The `src/UI/` folder is a webpack/React/TypeScript project It bundles to `district-groups.mjs`, which the game auto-loads from the mod's deploy folder. React and the `cs2/*` modules are game-provided externals; type definitions live in `src/UI/types/`.

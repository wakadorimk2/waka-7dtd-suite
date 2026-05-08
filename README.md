# Waka 7DTD Suite

A curated 7 Days to Die (v2.6) mod environment plus a collection of custom Harmony / XPath mods focused on **survival depth, decision density, and pet-presence**.

## What's in this repo

- `mods/` — source for every custom Waka* mod. Each subfolder is one mod, buildable independently from its `.csproj`.
- `modlist.txt` — the full Mod Organizer 2 load order this suite is tested against.
- `manifest.csv` — third-party mods this suite assumes are installed. Each row points to the Nexus ID; download them yourself (this repo does not redistribute third-party content).
- `docs/` — modlist organization notes and known issues.
- `scripts/` — PowerShell helpers for deploying built DLLs into a local MO2 layout.

Pre-built DLLs are **not** committed. Clone the repo, build the C# projects under `mods/<ModName>/Source/`, and the deploy script will copy artifacts into your MO2 mods folder.

## Why this exists

Most 7DTD overhauls are all-or-nothing — pick one and your whole game changes. This suite is the opposite: small, targeted patches that compose with established overhauls (Bloodfall, RAM, POI Scourge, Quest Revamp, EFTX/IZY, Medical Conditions, Sleep Overhaul, etc.) and adjust their friction without replacing them.

The design axis: **raise per-minute decision density without sacrificing the survival skeleton.**

## Custom mods (current state)

| Mod | Purpose | Status |
|---|---|---|
| WakaBodyLayer | Three-axis nutrition (protein/carb/fat) + four-region body damage layer | v0.7, in active tuning |
| WakaBodyLayerCATUI | CATUI bridge for the BodyLayer HUD | v0.1, working |
| WakaPerkBoost | Perk normalization patch (Bloodfall ZV dedup + vanilla buff) | v0.5 design |
| WakaQuestProgression | Tier-aware Scourge beacon quest fan-out + Quest Revamp counter integration | v0.3 |
| WakaQuestDifficultyTuning | tier-based GS scaling instead of FNS flat 10 | v0.4 |
| WakaSleepWorkstationFreeze | Stops Sleep Overhaul time-skip from advancing workstations | v0.1 built |
| WakaRamNullGuard | Harmony Prefix patches for RAM null-checks | v0.3 built |
| WakaCreAttachPatch | Cre_More integration with DO + Black Wolf-style conditional bonuses | v0.2 |
| WakaSlayerEZSPatch | EZS / NPCCore load order conflict fix | v0.1 |
| WakaResourceBridge / WakaMedFoodBridge / WakaGunFlowPatch / WakaFoodStatFix | Small XML compatibility/normalization patches | shipping |
| WakaJpLocPatch | Japanese localization patch (consolidated, in progress) | v0.1 WIP |
| WakaPet | Stationary AI pet (motion language + voice + LLM brain plans) | WIP, not for general use yet |

## Building

Each `mods/<Name>/Source/` directory contains a `.csproj`. Reference 7DTD's managed DLLs (`Assembly-CSharp.dll` etc., found under your game's `7DaysToDie_Data/Managed/`).

```pwsh
cd mods/WakaRamNullGuard/Source
dotnet build -c Release
# output DLL ends up under bin/Release/
```

After building, run `scripts/deploy.ps1` to copy DLLs into your local MO2 mods folder.

## Reproducing the modlist

1. Install Mod Organizer 2 (portable mode) under e.g. `C:\Modding\MO2\`.
2. Install 7 Days to Die through Steam.
3. For each row in `manifest.csv`, download the matching Nexus mod (matching the recorded version) and install it via MO2.
4. Replace `profiles/Default/modlist.txt` in your MO2 install with this repo's `modlist.txt`.
5. Build Waka mods (see "Building"), deploy them.
6. Launch through MO2 with EAC disabled.

See `docs/known-issues.md` for traps that have already cost time.

## License

MIT — see `LICENSE`. Custom assets that ship inside individual mods may have their own terms; check each mod's folder for a `LICENSE` or `README.md` override if present.

## Author

[wakadorimk2](https://github.com/wakadorimk2) — 7DTD modder, indie game developer (Quiet Days).

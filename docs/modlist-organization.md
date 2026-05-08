# Modlist organization

The `modlist.txt` in this repo groups mods by domain for legibility, with separators rather than priority tiers.

## Sections (top of file → bottom of file = MO2 priority high → low)

| # | Separator | Purpose |
|---|---|---|
| 1 | `自作_separator` | Custom Waka* mods (this repo's contribution). Kept at top so they override / supplement everything below. |
| 2 | `ストレージ_separator` | Storage / inventory: Lock Slot, Asylum 2-pack, NHX Backpacks, CATUI stack |
| 3 | `Guns_separator` | EFT / IZY weapon ecosystem (load-order sensitive internally — preserve subsections Z3 → Z2 → Z1 → Pack_Core → Pack Standard → Hard Req) |
| 4 | `Contents_separator` | Item / armor / vehicle additions |
| 5 | `戦闘_separator` | Combat & zombies: EZS, RAM, Bloodfall, More Gore, Damage Numbers, Kill Notification |
| 6 | `クエスト_separator` | Quest mods. Internal priority order matters: More Quest Options → POI Scourge → ExtraSlayerChallenges → Quests Per Tier 20 → Quest Revamp → FNS Make Quest Rewards Great Again |
| 7 | `サバイバル_separator` | Survival systems: Medical Conditions, Sleep Overhaul, Durability, WalkerSim, Fast Travel |
| 8 | `進行_separator` | Progression / skills: Research, Skill Magazine Crafting, PewPewLearnbyDoing |
| 9 | `Tweaks_separator` | Misc tweaks & QoL: Black Wolf 8-pack, Vehicle Speed, Workstation Timer, FPV Legs/FOV, Better Mod Compatibility, TMO Performance Plus |
| 10 | `前提_separator` | Frameworks: Quartz, KF Common Utility Library, Gears, NPCCore, 0-SCore, OCB Custom Textures, particle/action loaders, TFP_Harmony (bundled in IZY) |

## Why this layout instead of priority-tier grouping

Past layouts grouped mods by "tweak vs overhaul vs framework." That works for small modlists but stops scaling once you have ~80 mods spread across multiple domains. Domain grouping makes "where do I put a new quest mod" answerable in one read — and keeps related mods next to each other when troubleshooting load-order conflicts.

## What's load-bearing

A few placements actually matter:

- **EFT/IZY internal order** — Z3/Z2/Z1 prefixed mods stack on top of `EFTX_Pack_Core` → `EFTX Pack Standard` → `EFTX Hard Req`. Don't shuffle inside `Guns_separator`.
- **Black Wolf 8-pack** — keep contiguous so any conflict resolution between them is in one place.
- **Quest priority chain** — More Quest Options sits above the rest because it provides the framework everything else patches into.
- **Frameworks at the bottom** — Quartz / KF lib / 0-SCore / etc. need to load before consumers (which they do because consumers sit above them in MO2 priority, i.e. earlier in the file).

## What MO2 priority does *not* affect

Important not to over-think this: **MO2 priority** only resolves file-level conflicts (two mods shipping the same file path). For 7DTD's XML modlet system (`<append>`, `<set>`, `<remove>`), patches are applied in **inner-folder alphabetical order**, not MO2 priority order. So shuffling sections in `modlist.txt` rarely changes runtime behavior — it's mostly an organizational concern.

When two mods do conflict on the same physical file, MO2 priority decides. Those cases are rare, but worth checking with `Conflicts` in the MO2 GUI before assuming a reorganization is safe.

## Orphaned separators

Three legacy separators are kept around as folders but are no longer referenced in the active modlist: `UI_separator`, `Gameplay_separator`, `Overhaul_separator`. They were merged into the domain-grouped layout above. Delete the folders in MO2 if they bother you visually.

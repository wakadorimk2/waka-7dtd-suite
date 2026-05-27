# Bloodfall/RAM Waka patch audit

Date: 2026-05-28

Scope: issue #3, static audit of Bloodfall/RAM-dependent Waka patch folders after the Waka Lean EFTX World v2 baseline removed Bloodfall and RAM from both `profiles/Default/modlist.txt` and `profiles/NotebookServer/modlist.txt`.

No mod folders, XML files, profile files, or deploy targets were changed for this audit.

## Baseline status

| Mod / patch folder | Default | NotebookServer | Current baseline action |
| --- | --- | --- | --- |
| `Bloodfall - Hardcore Overhaul` | disabled | disabled | excluded |
| `RAM - Random Affixes Mod` | disabled | disabled | excluded |
| `ZZZZZZZ_WakaBloodfallRecipeFix v0.1` | disabled | disabled | keep disabled |
| `ZZZZZZZ_WakaBloodfallTuning v0.1` | disabled | disabled | keep disabled |
| `ZZZZZZZ_WakaRadiatedRegenTuning v0.1` | disabled | disabled | revise candidate, keep disabled as-is |
| `ZZZZZZZZZZ_Waka_Bloodfall_EFTX_SpawnBridge` | disabled | disabled | revise candidate for enemy-role work, keep disabled as-is |
| `ZZZ_WakaRamNullGuard v0.1` | disabled | disabled | keep disabled |
| `ZZZ_WakaRamAffixCurve v0.1` | disabled | disabled | keep disabled |

## Findings

| Folder | Files inspected | Dependency / target | EFTX/EZS baseline purpose | Classification | Notes |
| --- | --- | --- | --- | --- | --- |
| `ZZZZZZZ_WakaBloodfallRecipeFix v0.1` | `ZZZZZZZ_WakaBloodfallRecipeFix/ModInfo.xml`; `Config/recipes.xml` | Bloodfall-only recipes `Hellpick`, `Hellaxe`, `Hellshovel` | None while Bloodfall is excluded | disable / archive in place | The patch only adds `learnable` to Bloodfall Hell tool recipes. Re-enabling without Bloodfall would target missing recipe names. |
| `ZZZZZZZ_WakaBloodfallTuning v0.1` | `ZZZZZZZ_WakaBloodfallTuning/ModInfo.xml`; `Config/entitygroups.xml` | Bloodfall `Brutal*`, `Alpha*`, `Prime*`, and later tier entity names in biome wandering groups | None while Bloodfall is excluded | disable / archive in place | The file is explicitly a Bloodfall biome wandering rebalance. It removes Bloodfall prefixes and re-adds tier curves by biome. It should not be part of the lean EFTX/EZS baseline. |
| `ZZZZZZZ_WakaRadiatedRegenTuning v0.1` | `ZZZZZZZ_WakaRadiatedRegenTuning/ModInfo.xml`; `README.md`; `Config/entityclasses.xml` | Mixed vanilla, EZS T2-T5, and Bloodfall/boss radiated regen `RadiatedRegenAmount` values | Partial: vanilla/EZS regen tuning may still be useful | revise before any re-enable | This is the only audited Bloodfall-era XML patch with a plausible non-Bloodfall purpose. The current file also contains Bloodfall-only and boss target lines, so the safe path is a new or revised EZS-only regen patch rather than enabling this one unchanged. |
| `ZZZZZZZZZZ_Waka_Bloodfall_EFTX_SpawnBridge` | `ZZZZZZZZZZ_Waka_Bloodfall_EFTX_SpawnBridge/ModInfo.xml`; `Config/entitygroups.xml` | Adds EFTX special zombie names into late-game blood moon, wasteland, and high-tier sleeper groups for a Bloodfall late-game stack | Not as-is | revise candidate for issue #5 enemy-role design | The idea of light special-zombie pressure may be reusable, but the weights and pools were tuned as a Bloodfall bridge. Static search only found `zombieBikerBomber`, `zombieSoldierLootGob`, and `zombieDemolitionLootGob` in EFTX localization, and did not find entityclass definitions for those names in the active EFTX folders; `zombieDemolitionGiant` was only found in this bridge. Do not enable without merged XML/runtime validation. |
| `ZZZ_WakaRamNullGuard v0.1` | `ZZZ_WakaRamNullGuard/ModInfo.xml`; `WakaRamNullGuard.dll`; `Source/Init.cs`; `Source/RamBridge.cs`; `Source/Harmony/*.cs` | RAM assembly `WeaponAffixesProject`; defensive Harmony guards for RAM null paths | None while RAM is excluded | disable / archive in place | The code resolves RAM by reflection and self-skips when RAM is absent, but it has no gameplay purpose without RAM and still adds an unnecessary DLL mod to the baseline. |
| `ZZZ_WakaRamAffixCurve v0.1` | `ZZZ_WakaRamAffixCurve/ModInfo.xml`; `WakaRamAffixCurve.dll`; `Source/Init.cs`; `Source/RamCurveBridge.cs`; `Source/Harmony/CountModsToApplyPatch.cs` | RAM assembly `WeaponAffixesProject`; quality-based affix slot curve | None while RAM is excluded | disable / archive in place | The patch is a RAM balance override. It should stay off unless RAM returns as an explicit future design choice. |

## Cross-checks

- Both active profiles already have all audited Bloodfall/RAM patch folders disabled, matching `docs/modlist-baseline-lean-eftx-v2.md`.
- `Enhanced Zombie Scaling (2.5 Compatible)` is enabled and contains many `RadiatedRegenAmount` values, so an EZS-only regen follow-up may be legitimate if radiated sustain remains too high.
- Active Waka patches with Bloodfall entity strings also exist outside this issue's exact folder list:
  - `ZZZZZ_WakaSlayerEZSPatch v0.1`
  - `ZZZZZZZ_WakaBiomeChallengeFix v0.1`
  - `ZZZZZ_WakaTierCurve v0.1`
- Those active patches appear to use Bloodfall names as optional challenge/tier coverage strings or comments, not as direct Bloodfall XML xpaths in this audit. They should be reviewed under issue #5 or challenge-progression follow-up if exact lean-baseline cleanup is desired.

## Decision

Keep the current disabled state for every audited Bloodfall/RAM patch.

Do not delete the folders. They are useful historical references and may be safer than reconstructing the prior Bloodfall/RAM tuning from memory.

Move only two ideas forward:

- `WakaRadiatedRegenTuning`: revise into an EZS/vanilla-only regen patch if radiated sustain is still flattening ammo choices.
- `Waka_Bloodfall_EFTX_SpawnBridge`: treat as reference material for issue #5 enemy-role design, but rebuild from current EFTX/EZS entity definitions rather than re-enabling the existing bridge.

## Suggested issue outcomes

- `#3`: can be closed after this audit is accepted.
- `#4`: proceed with ammo role verification; no Bloodfall/RAM patch needs to be re-enabled first.
- `#5`: when designing enemy pressure, start from current EFTX/EZS entity definitions and only borrow the spawn-bridge concept if the referenced entity names validate in merged XML.

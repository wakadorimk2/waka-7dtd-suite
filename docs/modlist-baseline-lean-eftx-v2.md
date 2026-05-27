# Waka Lean EFTX World v2 modlist baseline

Date: 2026-05-28

Scope: baseline audit for `profiles/Default/modlist.txt` and `profiles/NotebookServer/modlist.txt` after removing the Bloodfall/RAM-heavy direction from the next-save plan.

## Decision

`ZZZZZZZZZZ_WakaAmmoRoleBalance v0.1` is now enabled in both profiles.

Reason: this patch changes shared item, recipe, and quest reward XML for the EFTX/IZY ammo role split. It affects server-authoritative balance such as ammo values, ammo press recipes, and Slayer rewards, so it should not remain client-only.

NotebookServer backup before edit:

- `profiles/NotebookServer/modlist.txt.bak.20260528-034149`

## Exclusion status

| Mod / system | Default | NotebookServer | Baseline decision |
| --- | --- | --- | --- |
| `Bloodfall - Hardcore Overhaul` | disabled | disabled | excluded |
| `RAM - Random Affixes Mod` | disabled | disabled | excluded |
| `FER Ore processing factories v2.6` | absent | disabled | excluded |
| `WalkerSim` | disabled | disabled | excluded |
| `Durability Overhaul` | disabled | disabled | excluded |
| `ZZZZZZZ_WakaBloodfallRecipeFix v0.1` | disabled | disabled | audit before re-enable |
| `ZZZZZZZ_WakaBloodfallTuning v0.1` | disabled | disabled | keep disabled |
| `ZZZZZZZ_WakaRadiatedRegenTuning v0.1` | disabled | disabled | keep disabled unless reused for non-Bloodfall role tuning |
| `ZZZZZZZZZZ_Waka_Bloodfall_EFTX_SpawnBridge` | disabled | disabled | keep disabled |
| `ZZZ_WakaRamNullGuard v0.1` | disabled | disabled | keep disabled while RAM is excluded |
| `ZZZ_WakaRamAffixCurve v0.1` | disabled | disabled | keep disabled while RAM is excluded |

## Remaining profile differences

The remaining differences after enabling `WakaAmmoRoleBalance` on NotebookServer are intentional or pending-review differences, not blockers for the #2 baseline.

| Entry | Default | NotebookServer | Classification |
| --- | --- | --- | --- |
| `ZZZZZZZZ_WakaToolbelt16Sync v0.1` | enabled | absent | client/toolbelt sync candidate; keep out of NotebookServer unless toolbelt slots are server-required |
| `ZZZ_CATUI_toolbelt_more_slot` | enabled | disabled | client UI/toolbelt difference; keep as client-only for now |
| `Black Wolf's better vanilla ground vehicles (1.0 and 2.0)` | enabled | absent | pending server-profile delta; not part of Bloodfall/RAM exclusion |
| `FER Ore processing factories v2.6` | absent | disabled | explicitly excluded on NotebookServer |
| `Knight's Combat Armors` | absent | disabled | server-profile disabled leftover |
| `LittleRedSonja Cosmetic Armor System (CAS)` | absent | disabled | server-profile disabled leftover |
| `(TMO) Better Turrets v2.1_v2.5ST` | absent | disabled | server-profile disabled leftover |
| `ZZZZZZZ_WakaBagLayerGear v0.1` | absent | disabled | server-profile disabled leftover |

## Validation

Commands run:

```powershell
& 'C:\Modding\MO2\tools\waka-deploy\deploy.ps1' 'C:\Modding\MO2\profiles\Default' -DryRun
& 'C:\Modding\MO2\tools\waka-deploy\deploy.ps1' 'C:\Modding\MO2\profiles\NotebookServer' -DryRun
```

Results:

| Profile | Enabled mods | Missing MO2 folders | No ModInfo | Dry-run result |
| --- | ---: | ---: | ---: | --- |
| Default | 109 | 0 | 0 | completed |
| NotebookServer | 106 | 0 | 0 | completed |

The dry-run output still reported many `TARGET EXISTS (real folder)` skips because the local game `Mods` folder contains real folders. That is an existing local deployment-state issue, not a missing-mod issue. No deploy was performed by these dry-run commands.

## Notebook deployment

Notebook deployment was performed after the baseline decision. Full `-FromProfile -Apply` was attempted first, but the remote remove step hit the known Windows `コマンド ラインが長すぎます` failure before copy. The server process had already been stopped, so the deploy was continued with a short explicit target list.

Command used for the successful apply/restart path:

```powershell
& 'C:\Modding\MO2\tools\waka-deploy\deploy-notebook.ps1' -Mod 'ZZZZZZZZZZ_WakaAmmoRoleBalance v0.1' -RemoveMod 'ZZZ_CATUI_toolbelt_more_slot' -Apply -Restart -Verify
```

The wrapper copied `ZZZZZZZZZZ_WakaAmmoRoleBalance`, confirmed `ZZZ_CATUI_toolbelt_more_slot` absent, and started one dedicated-server process. The wrapper's final verify step also hit the same command-length issue, so verification was completed with shorter encoded SSH probes.

Remote evidence:

| Check | Result |
| --- | --- |
| Process count | one `7DaysToDieServer.exe` |
| Process PID | `24348` |
| Latest log | `output_log_dedi__2026-05-28__03-44-40.txt` |
| Target mod load | `Trying to load from folder: 'ZZZZZZZZZZ_WakaAmmoRoleBalance'`; `Loaded Mod: ZZZZZZZZZZ_WakaAmmoRoleBalance (0.1)` |
| Startup | `World.Load: Xibibu Mountains`; `StartGame done`; `Server registered`; `GameServer.LogOn successful` |
| Target mod issues | no `ERR`, `WRN`, `XPath`, `XML`, `ModInfo`, or `Exception` lines naming `WakaAmmoRoleBalance` |
| Known noise | headless/null-GPU shader errors and unrelated Harmony warnings remain present |

## Next handoff

1. #3 should audit Bloodfall/RAM Waka patches as keep/revise/disable without deleting folders.
2. #4 should treat `WakaAmmoRoleBalance` as already implemented candidate A/C and verify merged XML plus runtime logs before notebook deployment.
3. Remote NotebookServer received the changed `WakaAmmoRoleBalance` target and was restarted. Full `-FromProfile -Apply` still needs the short-target workaround when command length is a risk.

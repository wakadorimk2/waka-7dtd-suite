# Next save preflight

Date: 2026-05-28

Scope: issue #12. Consolidates the final lean-baseline checks after resolving the Ore Processing, Durability Overhaul, and WalkerSim follow-up issues.

## Decisions carried forward

| Area | Decision | Evidence |
| --- | --- | --- |
| Ore Processing | Keep excluded. No new Waka patch before the next save. | `docs/ore-processing-post-removal-economy-20260528.md` |
| Durability Overhaul | Keep excluded. Disable `ZZZZZZZ_WakaDurabilityEquipFix v0.1` in both profiles. | `docs/durability-overhaul-exclusion-20260528.md` |
| WalkerSim | Keep excluded. Do not add broad spawn inflation. | `docs/walkersim-exclusion-pressure-20260528.md` |

## Profile and deploy changes

- Created backups before editing modlists:
  - `profiles/Default/modlist.txt.bak.20260528-084906`
  - `profiles/NotebookServer/modlist.txt.bak.20260528-084906`
- Changed `ZZZZZZZ_WakaDurabilityEquipFix v0.1` from enabled to disabled in:
  - `profiles/Default/modlist.txt`
  - `profiles/NotebookServer/modlist.txt`
- Local Default deploy completed after the profile change:
  - `Linked: 123`
  - `Missing: 0`
  - `NoModInfo: 0`
- Notebook deploy removed remote `Mods\ZZZZZZZ_WakaDurabilityEquipFix`, restarted the server, and verified the folder was absent.

## Runtime issue found during preflight

The first notebook runtime pass found `ZZZZZZZZZZ_WakaAmmoRoleBalance` XML warnings in `items.xml`, `recipes.xml`, and `quests.xml`.

Fixes applied:

- `items.xml`: removed no-op `TargetArmor` removals and corrected thrown explosive `Explosion.*` xpaths from `Action1` child properties to item-level properties.
- `recipes.xml`: removed broad setters/appends that targeted non-existent Ammo Press ingredients or recipes.
- `quests.xml`: removed broad reward setters for reward categories that are not present in the active Slayer quest rewards.

This keeps the intended active changes on real targets and removes startup warning noise from optional targets that do not exist in the current lean baseline.

## Validation

| Check | Result |
| --- | --- |
| `WakaAmmoRoleBalance` XML parse | OK for `items.xml`, `recipes.xml`, and `quests.xml`. |
| Default `waka-deploy -DryRun` | `Missing: 0`, `NoModInfo: 0`. |
| NotebookServer `waka-deploy -DryRun` | `Missing: 0`, `NoModInfo: 0`. |
| Default local deploy | `Linked: 123`, `Missing: 0`, `NoModInfo: 0`. |
| Notebook deploy of `WakaAmmoRoleBalance` | Copy and restart succeeded; wrapper verify hit the known command-line length limit. |
| Notebook process proof | `7DaysToDieServer.exe` restarted as PID `24252`. |
| Notebook log proof | Latest log `output_log_dedi__2026-05-28__08-57-26.txt` reached `StartGame` and `StartAsServer`. |
| `WakaAmmoRoleBalance` runtime warnings | None after the fix. The mod loaded at log lines 457-458. |
| `WakaDurabilityEquipFix` runtime presence | Not loaded in the latest notebook log. |

## Known unrelated runtime noise

The latest notebook log still has the existing headless shader errors and known mod warnings:

- Headless dedicated-server shader errors.
- Harmony lookup warnings around `Localization.Get` and `Quest.ID` / `Quest.id`.
- Gears saved-settings warnings for `Packwisely`, `CustomFpvFov`, and `FPVLegs`.
- Existing `BetterFire` warning for `ammoRocketFrag` `Explosion.BlockDamage`.

These are not introduced by this pass.

## Remaining caveat

A fresh local client runtime log after the final local deploy was not captured in this pass. Local deploy succeeded, and the notebook runtime proof is clean for the edited Waka patch. If a final client-side screenshot/log proof is needed before starting the save, run the client once and inspect the newest `%APPDATA%\7DaysToDie\logs\output_log_client__*.txt` for `WakaAmmoRoleBalance`, `WakaDurabilityEquipFix`, `XML patch`, `WRN`, and `ERR`.

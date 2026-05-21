# Current State

Generated: 2026-05-22 02:45 JST

This is a working snapshot for the live MO2 workspace. Treat it as a replaceable note: refresh it before making a new behavior change.

## Profile Snapshot

- Workspace: `C:\Modding\MO2`
- Active profile: `profiles\Default`
- Profile source: `profiles\Default\modlist.txt`
- Enabled entries: 118
- Disabled non-separator entries: 23
- Separators: 15
- Enabled Waka entries: 39
- Disabled Waka entries: 12

## Current Git State

Uncommitted changes already existed before this documentation pass:

- `mods/ZZZZZZZZ_WakaLogisticsLifePatch v0.1/ZZZZZZZZ_WakaLogisticsLifePatch/Config/Localization.txt`
- `mods/ZZZZZZZZ_WakaLogisticsLifePatch v0.1/ZZZZZZZZ_WakaLogisticsLifePatch/Config/recipes.xml`
- `mods/ZZZZZZZZ_WakaStorageRebalanceMVP v0.1/ZZZZZZZZ_WakaStorageRebalanceMVP/ModInfo.xml`
- `mods/ZZZZZZZZ_WakaStorageRebalanceMVP v0.1/ZZZZZZZZ_WakaStorageRebalanceMVP/README.md`
- `profiles/Default/modlist.txt`

Do not revert or normalize these without explicit direction.

## Deploy Snapshot

Latest inspected `tools\waka-deploy\deploy.log` summary:

- Timestamp in log tail: `02:24:05`
- Linked: 132
- Skipped: 2
- Missing MO2 folders: 0
- No `ModInfo.xml`: 0

This proves only the last deploy-script run, not the current game process state. For live proof, inspect the client or dedicated-server log after deployment.

## Runtime Logs

Recent client logs are under `%APPDATA%\7DaysToDie\logs`.

Latest inspected entries:

- `output_log_client__2026-05-22__02-30-22.txt` was 0 bytes at inspection time.
- `output_log_client__2026-05-22__02-08-21.txt` was the latest non-empty client log.

Workspace MO2 logs are under `logs/`; latest inspected entries were `mo_interface.log` and `usvfs-2026-05-21_15-42-28.log`.

## State Checks

Local client reflection:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -DryRun
& "tools\waka-deploy\deploy.ps1" "profiles\Default"
Get-ChildItem "$env:APPDATA\7DaysToDie\logs" -Filter "output_log_client__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime, Length
```

Local dedicated-server reflection:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -DryRun
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
```

Notebook dedicated-server reflection:

- Use the dedicated notebook sync/restart skill notes before touching the remote server.
- Previously working SSH target: `wakad@192.168.1.14`
- Remote dedicated path: `C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server`
- Verify by copying real mod folders, restarting through the scheduled-task helper, and reading the newest `output_log_dedi__*.txt`.

## First Places To Look Next Time

- `AGENTS.md`: workspace rules, shell workaround, deploy and remote notes.
- `CLAUDE.md`: mod-selection and stack judgment notes.
- `profiles\Default\modlist.txt`: enabled/disabled source of truth.
- `tools\waka-deploy\deploy.log`: last deploy-script result.
- `%APPDATA%\7DaysToDie\logs`: current client runtime proof.
- `_analysis\`: audit outputs such as economy and performance notes.
- `overwrite\`: check before moving generated files into a mod.

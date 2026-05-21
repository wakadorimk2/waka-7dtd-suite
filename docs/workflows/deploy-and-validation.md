# Deploy And Validation

This workspace is a live Mod Organizer 2 profile for 7 Days to Die. Deployment means reflecting enabled MO2 profile entries into a real game `Mods` folder; it is not a git or package release.

## Local Client Deploy

Preview first when there is any uncertainty:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -DryRun
```

Deploy to the normal client install:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default"
```

The deploy script removes its own old junctions, preserves real folders, and then creates junctions for enabled profile entries. It writes `tools\waka-deploy\deploy.log`.

## Local Dedicated Server Deploy

Preview:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -DryRun
```

Deploy:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
```

Start or restart the dedicated server only when the task explicitly calls for it. The local dedicated server install has its own `Mods`, `serverconfig.xml`, and logs.

## Notebook Dedicated Server

For the separate Windows notebook server, use the dedicated sync/restart notes in `skills/7dtd-dedicated-mod-sync-restart/SKILL.md` when available.

Stable facts to re-check before acting:

- SSH target previously used: `wakad@192.168.1.14`
- Remote path: `C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server`
- Use real copied mod folders on the remote server, not local MO2 junctions.
- Do not launch `startdedicated.bat` directly over SSH. Use the scheduled-task helper pattern and then inspect the newest dedicated log.

## Client Log Validation

Find recent logs:

```powershell
Get-ChildItem "$env:APPDATA\7DaysToDie\logs" -Filter "output_log_client__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime, Length
```

Scan the latest non-empty log:

```powershell
Select-String -Path "<latest-log-path>" -Pattern "ERR|WRN|XPath|XML|ModInfo|Exception" |
  Select-Object -First 120
```

For a specific Waka patch, also search for its deployed mod name or a distinctive block, item, buff, entity, or Harmony class name.

## Dedicated Log Validation

Find recent logs:

```powershell
Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -Filter "output_log_dedi__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime, Length
```

Scan the latest non-empty log:

```powershell
Select-String -Path "<latest-dedi-log-path>" -Pattern "ERR|WRN|XPath|XML|ModInfo|Exception" |
  Select-Object -First 160
```

Treat warnings as triage signals, not automatic regressions. Confirm whether the warning names the mod being changed, whether the server reached world load, and whether the same warning existed before the change.

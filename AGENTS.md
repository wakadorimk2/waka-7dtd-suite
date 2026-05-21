# Repository Guidelines

## Environment Overview

This workspace is a live Mod Organizer 2 instance for 7 Days to Die modding, not the Mod Organizer 2 application source tree. Treat it as an operational modlist workspace.

Key paths:

- `mods/`: MO2-managed mod folders. Local compatibility and tuning patches commonly use `Waka` names and high `Z` prefixes.
- `profiles/Default/modlist.txt`: active profile load order and enabled/disabled state.
- `profiles/Default/*.ini`: profile-specific MO2 settings.
- `overwrite/`: MO2 overwrite output. Inspect before moving anything into a mod.
- `tools/waka-deploy/`: deployment helper that links enabled MO2 mods into the real 7DTD `Mods` folder.
- `logs/` and `%APPDATA%\7DaysToDie\logs\`: useful runtime logs when diagnosing XML load errors, warnings, or gameplay issues.
- `C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\`: installed 7DTD dedicated server. It has its own `Mods`, `serverconfig.xml`, and `startdedicated.bat`, separate from the client game folder.
- `_analysis/`, `_archive/`, `_decomp/`, `_tools/`: local analysis, archived references, decompiled snippets, and helper material. Do not treat these as shipped mods unless explicitly requested.

## Working Rules

Preserve the current modlist and user environment. Do not normalize unrelated files, reorder mods, delete backups, or clean generated folders unless the task explicitly asks for it.

Before changing mod behavior, inspect the nearest existing mod structure and patch style. Prefer small, isolated compatibility patches in their own `mods/ZZ...Waka...` folder over editing third-party mods directly. If an upstream mod must be edited, call that out clearly.

Before editing `profiles/Default/modlist.txt`, create a timestamped backup next to it unless the change is trivially reversible and the user explicitly asked for a direct edit.

Keep MO2 folder names and 7DTD mod folder names distinct. A single MO2 folder can contain either a direct `ModInfo.xml` or one or more child folders with `ModInfo.xml`; `waka-deploy` handles both cases.

## Shell Execution Notes

This workspace can trigger `windows sandbox: setup refresh failed with status exit code: 1` when commands are launched directly with `cwd=C:\Modding\MO2`, especially simple reads such as `Get-Content mods\...\file.xml`.

When that happens, do not retry the same command shape repeatedly. Prefer launching PowerShell from a neutral working directory such as `C:\tmp`, and use absolute paths back into this workspace:

```powershell
pwsh -NoProfile -Command "Get-Content -LiteralPath 'C:\Modding\MO2\mods\...\file.xml'"
```

For scripted reads, prefer `-LiteralPath` over path strings that rely on wildcard expansion unless wildcard behavior is explicitly needed. If `pwsh` is available on `PATH`, use `pwsh` rather than the full `C:\Program Files\PowerShell\7\pwsh.exe` path.

## Common Commands

Inspect the active profile:

```powershell
Get-Content profiles\Default\modlist.txt
Get-ChildItem mods -Directory | Sort-Object Name | Select-Object Name, LastWriteTime
```

Deploy enabled mods to the game `Mods` folder:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default"
```

Preview deployment without changing the game folder:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -DryRun
```

Use an explicit game install path when needed:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die"
```

Deploy enabled mods to the dedicated server `Mods` folder:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
```

Preview dedicated server deployment without changing the server folder:

```powershell
& "tools\waka-deploy\deploy.ps1" "profiles\Default" -GamePath "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -DryRun
```

Start the dedicated server from its install directory when explicitly requested:

```powershell
Set-Location "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
& ".\startdedicated.bat"
```

The deploy script removes existing junctions under the game `Mods` folder, keeps real folders, then creates junctions for enabled profile entries. It writes `tools/waka-deploy/deploy.log`.

## Codex CapFrameX MCP Notes

CapFrameX exposes its MCP server from the running CapFrameX app when MCP is enabled in `%APPDATA%\CapFrameX\Configuration\AppSettings.json`.

Known working local settings:

- `McpEnabled`: `true`
- `WebservicePort`: `1337`
- MCP endpoint: `http://localhost:1337/mcp`
- Codex MCP name: `capframex`

Check the current Codex registration:

```powershell
codex mcp list
codex mcp get capframex
```

If the MCP entry is missing, register it again:

```powershell
codex mcp add capframex --url http://localhost:1337/mcp
```

If Codex cannot see the tools, confirm CapFrameX is running and listening:

```powershell
Test-NetConnection -ComputerName localhost -Port 1337
Select-String -LiteralPath "$env:APPDATA\CapFrameX\Logs\CapFrameX_003.log" -Pattern "MCP|1337" | Select-Object -Last 20
```

After adding or changing MCP registration, start a new Codex session if the `cfx_*` tools do not appear in the current session.

## Remote Dedicated Server SSH Notes

Detailed notebook-server SSH, SCP, save-backup, scheduled-task restart, and log-verification procedures now live in `skills/7dtd-dedicated-mod-sync-restart/SKILL.md`.

Use that skill whenever the user asks to deploy, mirror, back up, restart, or validate the separate Windows notebook dedicated server. Keep only the stable headline facts here:

- The previously working SSH target was `wakad@192.168.1.14`; treat the IP as LAN-local and possibly stale.
- The remote dedicated server path is `C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server`.
- Use key-based SSH for unattended Codex operations; password prompts are not usable non-interactively.
- Do not launch `startdedicated.bat` directly over SSH. For automated restarts, use a temporary scheduled task that runs `powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\WakaStartDediHeadless.ps1"`, then verify the newest `output_log_dedi__*.txt`.
- For `scp`, write the remote Windows path with forward slashes and quote it.

## Modlist And Load Order

MO2 `modlist.txt` entries beginning with `+` are enabled, `-` are disabled, and separator entries ending in `_separator` are not deployed.

This workspace uses prefix ordering heavily. High `Z`/`Waka` patches are usually intended to load after base mods and bridge or override behavior. Do not rename folders or change prefix counts casually; names are part of conflict resolution and deployment order.

When adding a new local patch:

1. Create a focused folder under `mods/`.
2. Include a valid `ModInfo.xml` at the deployed mod root.
3. Put XML patches under the conventional 7DTD paths such as `Config/`.
4. Add the MO2 folder to `profiles/Default/modlist.txt` only when requested or when deployment is part of the task.
5. Run `waka-deploy` with `-DryRun` first when there is any uncertainty about conflicts.

## Validation

For XML changes, validate by checking the relevant files and, when practical, deploy and inspect the latest 7DTD log for `ERR`, `WRN`, missing xpath, duplicate item, or XML parse messages.

Useful log checks:

```powershell
Get-ChildItem "$env:APPDATA\7DaysToDie\logs" -Filter "output_log_client__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime
```

```powershell
Select-String -Path "<latest-log-path>" -Pattern "ERR|WRN|XPath|XML|ModInfo" |
  Select-Object -First 120
```

If a change affects progression, loot, traders, recipes, entities, buffs, or localization, prefer targeted inspection of the affected XML plus a runtime log check after deploy.

For dedicated server validation, deploy to the dedicated server path with `-DryRun` first, then deploy for real if the target list is correct. After startup, inspect `output_log_dedi__*.txt` in `C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\` and the server `logs\` folder for XML load errors, failed xpaths, missing mods, and startup warnings.

Useful dedicated server log checks:

```powershell
Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -Filter "output_log_dedi__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime
```

```powershell
Select-String -Path "<latest-dedi-log-path>" -Pattern "ERR|WRN|XPath|XML|ModInfo|Exception" |
  Select-Object -First 160
```

## Coding And File Style

Match the style of the mod being edited. Preserve XML indentation, xpath style, comments, and naming conventions already present nearby.

Use structured XML edits where possible. Avoid broad string rewrites or formatting churn. Keep compatibility patches readable and explain non-obvious xpath choices with short comments only when useful.

For PowerShell helper changes, preserve the existing script style, parameter names, and logging approach. Test with `-DryRun` when the script supports it.

## Safety

Do not delete installed mods, downloads, profiles, overwrite contents, or game files unless explicitly asked. Do not run destructive cleanup commands against `mods/`, `profiles/`, `overwrite/`, or the real game `Mods` folder without confirming the exact target.

Network access, Nexus API calls, game launches, and writes outside this workspace may require user approval. Ask only when needed, and prefer local inspection first.

## Response Style

Respond in Japanese, using polite ojou-sama style. Include 1-2 emoji when natural.

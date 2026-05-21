# State And Triage

Start from observed state. This workspace changes through MO2 profile edits, deploy-script reflection, live game logs, and occasional remote notebook-server sync.

## First Snapshot

```powershell
git status --short
Get-Content -LiteralPath "C:\Modding\MO2\profiles\Default\modlist.txt"
Get-ChildItem -LiteralPath "C:\Modding\MO2\overwrite" -Force
Get-Content -LiteralPath "C:\Modding\MO2\tools\waka-deploy\deploy.log" -Tail 80
```

If commands fail from the MO2 workdir, launch from `C:\tmp` and use absolute `-LiteralPath` reads.

## Git Status

Treat existing uncommitted changes as user work unless proven otherwise. Do not revert or reformat unrelated files.

Useful views:

```powershell
git -C C:\Modding\MO2 status --short
git -C C:\Modding\MO2 diff --stat
git -C C:\Modding\MO2 diff --check
```

For a dirty file that must be edited, read it first and make a minimal additive change.

## Overwrite

`overwrite\` can contain generated MO2 output or files produced by a tool. Inspect it before moving anything:

```powershell
Get-ChildItem -LiteralPath "C:\Modding\MO2\overwrite" -Force
```

Do not clean or move overwrite contents unless the task explicitly asks for it.

## Client Logs

Use the latest non-empty client log for runtime proof:

```powershell
Get-ChildItem "$env:APPDATA\7DaysToDie\logs" -Filter "output_log_client__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime, Length
```

Then scan for:

- `ERR`
- `WRN`
- `XPath`
- `XML`
- `ModInfo`
- `Exception`
- the exact Waka mod name

## Dedicated Logs

The local dedicated server is separate from the client install:

```powershell
Get-ChildItem "C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server" -Filter "output_log_dedi__*.txt" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime, Length
```

When validating remote notebook changes, use SSH and the dedicated sync/restart workflow rather than assuming the local deploy state applies remotely.

## Analysis Artifacts

`_analysis\` contains useful but potentially stale evidence. Prefer the newest relevant artifact, then decide whether to refresh it.

Known examples:

- `_analysis\waka-economy-risk-report.html`: sell-economy audit output.
- `_analysis\2026-05-22-performance-preflight.md`: recent performance preflight note.
- `tools\waka-economy-scan.ps1`: economy scanner source.
- `tools\waka-tier-curve-visualizer.html`: tier curve visualization.

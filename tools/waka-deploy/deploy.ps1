[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$ProfilePath,

    [string]$GamePath = 'C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die',

    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$ModlistPath = Join-Path $ProfilePath 'modlist.txt'
$Mo2Base     = Split-Path (Split-Path $ProfilePath)
$ModsRoot    = Join-Path $Mo2Base 'mods'
$GameMods    = Join-Path $GamePath 'Mods'
$LogPath     = Join-Path $PSScriptRoot 'deploy.log'
$ConfigPath  = Join-Path $PSScriptRoot 'deploy.config.psd1'

$exclude = @{}
if (Test-Path $ConfigPath) {
    $cfg = Import-PowerShellDataFile -Path $ConfigPath
    if ($cfg.Exclude) { $exclude = $cfg.Exclude }
}

$logLines = New-Object System.Collections.Generic.List[string]
function Log([string]$msg, [ConsoleColor]$color = [ConsoleColor]::Gray) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'HH:mm:ss'), $msg
    $logLines.Add($line)
    Write-Host $line -ForegroundColor $color
}

Log '=== Waka Deploy ==='
Log "Profile : $ProfilePath"
Log "Mods    : $ModsRoot"
Log "Game    : $GamePath"
Log "DryRun  : $DryRun"

if (-not (Test-Path $ModlistPath)) { throw "modlist.txt not found: $ModlistPath" }
if (-not (Test-Path $ModsRoot))    { throw "mods folder not found: $ModsRoot" }
if (-not (Test-Path $GameMods)) {
    Log "Creating game Mods folder" 'Yellow'
    if (-not $DryRun) { New-Item -ItemType Directory -Path $GameMods -Force | Out-Null }
}

Log '--- Cleanup phase ---' 'Cyan'
$removedJunctions = 0
if (Test-Path $GameMods) {
    Get-ChildItem $GameMods -Directory -Force | ForEach-Object {
        if ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            Log "  remove junction: $($_.Name)"
            if (-not $DryRun) {
                [System.IO.Directory]::Delete($_.FullName, $false)
            }
            $removedJunctions++
        } else {
            Log "  keep real folder: $($_.Name)" 'DarkGray'
        }
    }
}
Log "Cleanup done. Junctions removed: $removedJunctions"

Log '--- Parse modlist ---' 'Cyan'
$enabled = New-Object System.Collections.Generic.List[string]
Get-Content $ModlistPath | ForEach-Object {
    $line = $_.Trim()
    if ($line.StartsWith('+')) {
        $name = $line.Substring(1)
        if ($name -and -not $name.EndsWith('_separator')) {
            $enabled.Add($name)
        }
    }
}
Log "Enabled mods in modlist: $($enabled.Count)"

Log '--- Deploy phase (top-wins) ---' 'Cyan'
$claimed = @{}
$linked  = 0
$skipped = 0
$missing = 0
$noinfo  = 0

foreach ($mo2Mod in $enabled) {
    $mo2Path = Join-Path $ModsRoot $mo2Mod
    if (-not (Test-Path $mo2Path)) {
        Log "  MISSING: $mo2Mod" 'Red'
        $missing++
        continue
    }

    $realMods = @()
    if (Test-Path (Join-Path $mo2Path 'ModInfo.xml')) {
        $realMods = @(Get-Item $mo2Path)
    } else {
        $realMods = @(Get-ChildItem $mo2Path -Directory | Where-Object {
            Test-Path (Join-Path $_.FullName 'ModInfo.xml')
        })
    }

    if ($realMods.Count -eq 0) {
        Log "  no ModInfo.xml: $mo2Mod" 'DarkYellow'
        $noinfo++
        continue
    }

    $excludeList = @()
    if ($exclude.ContainsKey($mo2Mod)) { $excludeList = $exclude[$mo2Mod] }

    foreach ($real in $realMods) {
        $targetName = $real.Name

        if ($excludeList -contains $targetName) {
            Log "  excluded by config: $targetName from $mo2Mod" 'DarkGray'
            continue
        }

        $target = Join-Path $GameMods $targetName

        if ($claimed.ContainsKey($targetName)) {
            Log "  CONFLICT: '$targetName' already claimed by '$($claimed[$targetName])', skip '$mo2Mod'" 'Yellow'
            $skipped++
            continue
        }
        if (Test-Path $target) {
            Log "  TARGET EXISTS (real folder): $targetName, skip" 'Yellow'
            $skipped++
            continue
        }

        Log "  link: $targetName <- $mo2Mod" 'Green'
        if (-not $DryRun) {
            New-Item -ItemType Junction -Path $target -Value $real.FullName | Out-Null
        }
        $claimed[$targetName] = $mo2Mod
        $linked++
    }
}

Log '--- Summary ---' 'Cyan'
Log "Linked    : $linked"
Log "Skipped   : $skipped (conflicts or real-folder collisions)"
Log "Missing   : $missing (MO2 folder not found)"
Log "NoModInfo : $noinfo (no ModInfo.xml at depth 0 or 1)"

$logLines | Set-Content -Path $LogPath -Encoding UTF8
Log "Log written: $LogPath"

if ($DryRun) {
    Write-Host ''
    Write-Host '[DRY RUN] No changes were made.' -ForegroundColor Yellow
}

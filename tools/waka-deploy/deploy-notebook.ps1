[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)]
    [string[]]$Mod,

    [switch]$Apply,

    [switch]$Restart,

    [switch]$Verify,

    [string]$RemoteHost = 'wakad@192.168.1.14',

    [string]$RemoteDediPath = 'C:/Program Files (x86)/Steam/steamapps/common/7 Days to Die Dedicated Server'
)

$ErrorActionPreference = 'Stop'

$Mo2Base    = Split-Path (Split-Path $PSScriptRoot)
$ModsRoot   = Join-Path $Mo2Base 'mods'
$LogPath    = Join-Path $PSScriptRoot 'deploy-notebook.log'
$ConfigPath = Join-Path $PSScriptRoot 'deploy.config.psd1'
$RemoteModsPath = ($RemoteDediPath.TrimEnd('/\') + '/Mods')

$exclude = @{}
$excludeMods = @()
if (Test-Path $ConfigPath) {
    $cfg = Import-PowerShellDataFile -Path $ConfigPath
    if ($cfg.Exclude) { $exclude = $cfg.Exclude }
    if ($cfg.ExcludeMods) { $excludeMods = @($cfg.ExcludeMods) }
}

$logLines = New-Object System.Collections.Generic.List[string]
function Log([string]$Message, [ConsoleColor]$Color = [ConsoleColor]::Gray) {
    $line = '[{0}] {1}' -f (Get-Date -Format 'HH:mm:ss'), $Message
    $logLines.Add($line)
    Write-Host $line -ForegroundColor $Color
}

function Quote-RemotePsString([string]$Value) {
    return "'" + $Value.Replace("'", "''") + "'"
}

function New-RemoteEncodedCommand([string]$Script) {
    $bytes = [System.Text.Encoding]::Unicode.GetBytes($Script)
    return [Convert]::ToBase64String($bytes)
}

function Invoke-RemotePowerShell([string]$Script, [string]$Description) {
    Log $Description 'DarkCyan'
    $encoded = New-RemoteEncodedCommand $Script
    $args = @($RemoteHost, 'powershell.exe', '-NoProfile', '-EncodedCommand', $encoded)
    & ssh @args
    if ($LASTEXITCODE -ne 0) {
        throw "ssh failed ($LASTEXITCODE): $Description"
    }
}

function Invoke-ScpCopy([string]$SourcePath) {
    $target = ('{0}:"{1}/"' -f $RemoteHost, $RemoteModsPath)
    Log ("scp: {0} -> {1}" -f $SourcePath, $target) 'DarkCyan'
    & scp -r $SourcePath $target
    if ($LASTEXITCODE -ne 0) {
        throw "scp failed ($LASTEXITCODE): $SourcePath"
    }
}

function Get-RealModsFromOuter([System.IO.DirectoryInfo]$Outer, [switch]$Quiet) {
    if ($excludeMods -contains $Outer.Name) {
        if (-not $Quiet) {
            Log "  excluded by config: $($Outer.Name)" 'DarkGray'
        }
        return
    }

    $realMods = @()
    if (Test-Path (Join-Path $Outer.FullName 'ModInfo.xml')) {
        $realMods = @($Outer)
    } else {
        $realMods = @(Get-ChildItem -LiteralPath $Outer.FullName -Directory | Where-Object {
            Test-Path (Join-Path $_.FullName 'ModInfo.xml')
        })
    }

    $excludeList = @()
    if ($exclude.ContainsKey($Outer.Name)) { $excludeList = $exclude[$Outer.Name] }

    foreach ($real in $realMods) {
        if ($excludeMods -contains $real.Name) {
            if (-not $Quiet) {
                Log "  excluded by config: $($real.Name) from $($Outer.Name)" 'DarkGray'
            }
            continue
        }

        if ($excludeList -contains $real.Name) {
            if (-not $Quiet) {
                Log "  excluded by config: $($real.Name) from $($Outer.Name)" 'DarkGray'
            }
            continue
        }

        [pscustomobject]@{
            OuterName = $Outer.Name
            Name      = $real.Name
            FullName  = $real.FullName
        }
    }
}

function Resolve-RequestedMod([string]$Name) {
    if ($excludeMods -contains $Name) {
        throw "Mod is excluded by deploy.config.psd1 ExcludeMods: $Name"
    }

    $outerPath = Join-Path $ModsRoot $Name
    if (Test-Path -LiteralPath $outerPath -PathType Container) {
        $outer = Get-Item -LiteralPath $outerPath
        $realMods = @(Get-RealModsFromOuter $outer)
        if ($realMods.Count -eq 0) {
            throw "No ModInfo.xml found at depth 0 or 1 for MO2 folder: $Name"
        }
        return $realMods
    }

    $matches = New-Object System.Collections.Generic.List[object]
    Get-ChildItem -LiteralPath $ModsRoot -Directory | ForEach-Object {
        $outer = $_
        $realMods = @(Get-RealModsFromOuter $outer -Quiet | Where-Object { $_.Name -eq $Name })
        foreach ($real in $realMods) { $matches.Add($real) }
    }

    if ($matches.Count -eq 0) {
        throw "Mod not found as MO2 folder or inner 7DTD mod folder: $Name"
    }
    if ($matches.Count -gt 1) {
        $owners = ($matches | ForEach-Object { "$($_.Name) from $($_.OuterName)" }) -join '; '
        throw "Ambiguous inner mod name '$Name': $owners"
    }

    return @($matches[0])
}

function Stop-RemoteServer {
    $script = @'
$ErrorActionPreference = 'Stop'
$procs = @(Get-Process -Name '7DaysToDieServer' -ErrorAction SilentlyContinue)
if ($procs.Count -eq 0) {
    Write-Output 'SERVER_PROCESS none'
    return
}
foreach ($proc in $procs) {
    Write-Output ("SERVER_PROCESS stop PID={0}" -f $proc.Id)
    Stop-Process -Id $proc.Id -Force
}
Start-Sleep -Seconds 3
$remaining = @(Get-Process -Name '7DaysToDieServer' -ErrorAction SilentlyContinue)
Write-Output ("SERVER_PROCESS remaining={0}" -f $remaining.Count)
if ($remaining.Count -gt 0) { exit 2 }
'@
    Invoke-RemotePowerShell $script 'remote: stop 7DaysToDieServer.exe'
}

function Remove-RemoteMods([object[]]$Targets) {
    $quotedDedi = Quote-RemotePsString $RemoteDediPath.Replace('/', '\')
    $quotedNames = ($Targets | ForEach-Object { Quote-RemotePsString $_.Name }) -join ', '
    $script = @"
`$ErrorActionPreference = 'Stop'
`$modsRoot = Join-Path $quotedDedi 'Mods'
`$names = @($quotedNames)
foreach (`$name in `$names) {
    `$target = Join-Path `$modsRoot `$name
    if (Test-Path -LiteralPath `$target) {
        Write-Output ("REMOVE_MOD {0}" -f `$target)
        Remove-Item -LiteralPath `$target -Recurse -Force
    } else {
        Write-Output ("REMOVE_MOD absent {0}" -f `$target)
    }
}
"@
    Invoke-RemotePowerShell $script 'remote: remove selected mod folders'
}

function Start-RemoteServer {
    $quotedLauncher = Quote-RemotePsString (($RemoteDediPath.TrimEnd('/\') + '/WakaStartDediHeadless.ps1').Replace('/', '\'))
    $script = @"
`$ErrorActionPreference = 'Stop'
`$taskName = 'WakaStart7DtdDedi'
`$launcher = $quotedLauncher
if (-not (Test-Path -LiteralPath `$launcher)) { throw "Launcher not found: `$launcher" }
Unregister-ScheduledTask -TaskName `$taskName -Confirm:`$false -ErrorAction SilentlyContinue | Out-Null
`$action = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument ('-NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File "{0}"' -f `$launcher)
Register-ScheduledTask -TaskName `$taskName -Action `$action -Description 'Temporary Waka 7DTD dedicated start task' -Force | Out-Null
Start-ScheduledTask -TaskName `$taskName
Start-Sleep -Seconds 8
`$procs = @(Get-Process -Name '7DaysToDieServer' -ErrorAction SilentlyContinue)
Write-Output ("SERVER_PROCESS count={0}" -f `$procs.Count)
foreach (`$proc in `$procs) { Write-Output ("SERVER_PROCESS PID={0}" -f `$proc.Id) }
Unregister-ScheduledTask -TaskName `$taskName -Confirm:`$false -ErrorAction SilentlyContinue | Out-Null
if (`$procs.Count -ne 1) { exit 3 }
"@
    Invoke-RemotePowerShell $script 'remote: start server via scheduled task'
}

function Verify-RemoteLog([object[]]$Targets) {
    $quotedDedi = Quote-RemotePsString $RemoteDediPath.Replace('/', '\')
    $quotedNames = ($Targets | ForEach-Object { Quote-RemotePsString $_.Name }) -join ', '
    $script = @"
`$ErrorActionPreference = 'Stop'
`$dedi = $quotedDedi
`$names = @($quotedNames)
`$log = Get-ChildItem -LiteralPath `$dedi -Filter 'output_log_dedi__*.txt' -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if (-not `$log) { throw "No output_log_dedi__*.txt found in `$dedi" }
Write-Output ("LATEST_LOG {0} {1}" -f `$log.LastWriteTime.ToString('s'), `$log.FullName)
`$content = Get-Content -LiteralPath `$log.FullName -ErrorAction Stop
`$failed = `$false
foreach (`$name in `$names) {
    `$load = @(`$content | Select-String -SimpleMatch `$name | Where-Object {
        `$_.Line -match 'Trying to load from folder:|Loaded Mod:'
    } | Select-Object -First 20)
    if (`$load.Count -eq 0) {
        Write-Output ("VERIFY_MISSING_LOAD {0}" -f `$name)
        `$failed = `$true
    } else {
        foreach (`$line in `$load) { Write-Output ("VERIFY_LOAD {0}: {1}" -f `$name, `$line.Line.Trim()) }
    }

    `$issues = @(`$content | Select-String -SimpleMatch `$name | Where-Object {
        `$_.Line -match 'ERR|WRN|XPath|XML|ModInfo|Exception'
    } | Select-Object -First 40)
    foreach (`$line in `$issues) {
        Write-Output ("VERIFY_ISSUE {0}: {1}" -f `$name, `$line.Line.Trim())
    }
    if (`$issues.Count -gt 0) { `$failed = `$true }
}
if (`$failed) { exit 4 }
"@
    Invoke-RemotePowerShell $script 'remote: verify newest dedicated log'
}

try {
    if (-not (Test-Path -LiteralPath $ModsRoot -PathType Container)) {
        throw "mods folder not found: $ModsRoot"
    }

    Log '=== Waka Notebook Deploy ==='
    Log "MO2 mods       : $ModsRoot"
    Log "Remote host    : $RemoteHost"
    Log "Remote dedi    : $RemoteDediPath"
    Log "Remote Mods    : $RemoteModsPath"
    Log "Apply          : $Apply"
    Log "Restart        : $Restart"
    Log "Verify         : $Verify"

    $resolved = New-Object System.Collections.Generic.List[object]
    $claimed = @{}
    foreach ($requested in $Mod) {
        Log "--- Resolve: $requested ---" 'Cyan'
        $realMods = @(Resolve-RequestedMod $requested)
        foreach ($real in $realMods) {
            if ($claimed.ContainsKey($real.Name)) {
                throw "Duplicate target mod '$($real.Name)' requested by '$requested' and '$($claimed[$real.Name])'"
            }
            $claimed[$real.Name] = $requested
            $resolved.Add($real)
            Log ("  {0} <- {1}" -f $real.Name, $real.OuterName) 'Green'
            Log ("    local : {0}" -f $real.FullName)
            Log ("    remote: {0}/{1}" -f $RemoteModsPath, $real.Name)
        }
    }

    Log '--- Planned remote changes ---' 'Cyan'
    foreach ($real in $resolved) {
        Log ("  remove then copy: {0}/{1}" -f $RemoteModsPath, $real.Name)
    }
    if ($Restart) { Log '  restart: stop process, start WakaStartDediHeadless.ps1 via scheduled task' }
    if ($Verify) { Log '  verify: newest output_log_dedi__*.txt for target load and target-specific issues' }

    if (-not $Apply) {
        Log 'DryRun only. No ssh, scp, remote delete, copy, restart, or verify was executed.' 'Yellow'
        Write-Host ''
        Write-Host '[DRY RUN] Add -Apply to change the notebook server.' -ForegroundColor Yellow
        return
    }

    if ($Restart) { Stop-RemoteServer }
    Remove-RemoteMods @($resolved)
    foreach ($real in $resolved) {
        Invoke-ScpCopy $real.FullName
    }
    if ($Restart) { Start-RemoteServer }
    if ($Verify) { Verify-RemoteLog @($resolved) }

    Log 'Notebook deploy completed.' 'Green'
} catch {
    Log "ERROR: $($_.Exception.Message)" 'Red'
    throw
} finally {
    $logLines | Set-Content -Path $LogPath -Encoding UTF8
    Log "Log written: $LogPath"
}

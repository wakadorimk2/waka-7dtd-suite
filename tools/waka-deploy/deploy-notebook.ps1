[CmdletBinding()]
param(
    [string[]]$Mod = @(),

    [string[]]$RemoveMod = @(),

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
$notebookExcludeMods = @()
if (Test-Path $ConfigPath) {
    $cfg = Import-PowerShellDataFile -Path $ConfigPath
    if ($cfg.Exclude) { $exclude = $cfg.Exclude }
    if ($cfg.ExcludeMods) { $excludeMods = @($cfg.ExcludeMods) }
    if ($cfg.NotebookExcludeMods) { $notebookExcludeMods = @($cfg.NotebookExcludeMods) }
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

function Test-ModNameExcluded([string]$Name, [string[]]$List) {
    foreach ($excluded in $List) {
        if ($Name -eq $excluded -or $Name -like "$excluded v*") {
            return $true
        }
    }
    return $false
}

function Get-ModExcludeReason([string]$Name) {
    if (Test-ModNameExcluded $Name $notebookExcludeMods) {
        return 'NotebookExcludeMods'
    }
    if (Test-ModNameExcluded $Name $excludeMods) {
        return 'ExcludeMods'
    }
    return $null
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
    $target = ('{0}:{1}/' -f $RemoteHost, $RemoteModsPath)
    Log ("scp: {0} -> {1}" -f $SourcePath, $target) 'DarkCyan'
    & scp -r $SourcePath $target
    if ($LASTEXITCODE -ne 0) {
        throw "scp failed ($LASTEXITCODE): $SourcePath"
    }
}

function Get-RealModsFromOuter([System.IO.DirectoryInfo]$Outer, [switch]$Quiet) {
    $outerExcludeReason = Get-ModExcludeReason $Outer.Name
    if ($outerExcludeReason) {
        if (-not $Quiet) {
            Log "  excluded by config $($outerExcludeReason): $($Outer.Name)" 'DarkGray'
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
        $realExcludeReason = Get-ModExcludeReason $real.Name
        if ($realExcludeReason) {
            if (-not $Quiet) {
                Log "  excluded by config $($realExcludeReason): $($real.Name) from $($Outer.Name)" 'DarkGray'
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
    $excludeReason = Get-ModExcludeReason $Name
    if ($excludeReason) {
        throw "Mod is excluded by deploy.config.psd1 $($excludeReason): $Name"
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
    if (-not $Targets -or $Targets.Count -eq 0) { return }

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

function Remove-RemoteModNames([string[]]$Names) {
    if (-not $Names -or $Names.Count -eq 0) { return }

    $quotedDedi = Quote-RemotePsString $RemoteDediPath.Replace('/', '\')
    $quotedNames = ($Names | ForEach-Object { Quote-RemotePsString $_ }) -join ', '
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
    Invoke-RemotePowerShell $script 'remote: remove disabled mod folders'
}

function Verify-RemoteRemovedMods([string[]]$Names) {
    if (-not $Names -or $Names.Count -eq 0) { return }

    $quotedDedi = Quote-RemotePsString $RemoteDediPath.Replace('/', '\')
    $quotedNames = ($Names | ForEach-Object { Quote-RemotePsString $_ }) -join ', '
    $script = @"
`$ErrorActionPreference = 'Stop'
`$modsRoot = Join-Path $quotedDedi 'Mods'
`$names = @($quotedNames)
`$failed = `$false
foreach (`$name in `$names) {
    `$target = Join-Path `$modsRoot `$name
    if (Test-Path -LiteralPath `$target) {
        Write-Output ("VERIFY_REMOVE_PRESENT {0}" -f `$target)
        `$failed = `$true
    } else {
        Write-Output ("VERIFY_REMOVE_ABSENT {0}" -f `$target)
    }
}
if (`$failed) { exit 5 }
"@
    Invoke-RemotePowerShell $script 'remote: verify disabled mod folders are absent'
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
    if (-not $Targets -or $Targets.Count -eq 0) {
        Log 'remote: verify newest dedicated log skipped because there are no copied targets' 'DarkGray'
        return
    }

    $quotedDedi = Quote-RemotePsString $RemoteDediPath.Replace('/', '\')
    $quotedNames = ($Targets | ForEach-Object { Quote-RemotePsString $_.Name }) -join ', '
    $script = @"
`$ErrorActionPreference = 'Stop'
`$dedi = $quotedDedi
`$names = @($quotedNames)
`$deadline = (Get-Date).AddSeconds(180)
`$content = @()
`$log = `$null
`$hasWorldLoad = `$false
`$hasSteamLogon = `$false
do {
    `$log = Get-ChildItem -LiteralPath `$dedi -Filter 'output_log_dedi__*.txt' -File |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if (`$log) {
        `$content = @(Get-Content -LiteralPath `$log.FullName -ErrorAction Stop)
        `$hasWorldLoad = @(`$content | Select-String -SimpleMatch 'World.Load:' | Select-Object -First 1).Count -gt 0
        `$hasSteamLogon = @(`$content | Select-String -SimpleMatch 'GameServer.LogOn successful' | Select-Object -First 1).Count -gt 0
        if (`$content.Count -gt 0 -and `$hasWorldLoad -and `$hasSteamLogon) { break }
    }
    Start-Sleep -Seconds 5
} while ((Get-Date) -lt `$deadline)

if (-not `$log) { throw "No output_log_dedi__*.txt found in `$dedi" }
Write-Output ("LATEST_LOG {0} {1}" -f `$log.LastWriteTime.ToString('s'), `$log.FullName)
if (`$content.Count -eq 0) {
    Write-Output 'VERIFY_STARTUP log is empty after wait'
    exit 4
}

`$procs = @(Get-Process -Name '7DaysToDieServer' -ErrorAction SilentlyContinue)
Write-Output ("VERIFY_PROCESS count={0}" -f `$procs.Count)
foreach (`$proc in `$procs) { Write-Output ("VERIFY_PROCESS PID={0}" -f `$proc.Id) }

`$knownNullGfxNoise = @(`$content | Where-Object {
    (`$_ -match 'Shader .+ shader is not supported on this GPU') -or
    (`$_ -match 'Shader Unsupported: .+ All subshaders removed') -or
    (`$_ -match 'Did you use #pragma only_renderers') -or
    (`$_ -match 'graphics device is Null')
})
Write-Output ("VERIFY_CLASS Known NullGfx shader noise: {0} lines" -f `$knownNullGfxNoise.Count)

`$failed = `$false
if (`$procs.Count -ne 1) {
    Write-Output ("VERIFY_PROCESS issue: expected exactly one 7DaysToDieServer.exe, found {0}" -f `$procs.Count)
    `$failed = `$true
}

`$targetIssueCount = 0
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
        (`$_.Line -match 'ERR|WRN|XPath|XML|ModInfo|Exception') -and -not (
            (`$_.Line -match 'Shader .+ shader is not supported on this GPU') -or
            (`$_.Line -match 'Shader Unsupported: .+ All subshaders removed') -or
            (`$_.Line -match 'Did you use #pragma only_renderers') -or
            (`$_.Line -match 'graphics device is Null')
        )
    } | Select-Object -First 40)
    Write-Output ("VERIFY_CLASS Target mod issues for {0}: {1} lines" -f `$name, `$issues.Count)
    `$targetIssueCount += `$issues.Count
    foreach (`$line in `$issues) {
        Write-Output ("VERIFY_ISSUE {0}: {1}" -f `$name, `$line.Line.Trim())
    }
    if (`$issues.Count -gt 0) { `$failed = `$true }
}
Write-Output ("VERIFY_CLASS Target mod issues: {0} lines" -f `$targetIssueCount)
if (`$hasWorldLoad) {
    `$worldLoad = @(`$content | Select-String -SimpleMatch 'World.Load:' | Select-Object -First 1)[0].Line.Trim()
    Write-Output ("VERIFY_STARTUP reached: {0}" -f `$worldLoad)
} else {
    Write-Output 'VERIFY_STARTUP missing: World.Load'
    `$failed = `$true
}
if (`$hasSteamLogon) {
    Write-Output 'VERIFY_STARTUP reached: GameServer.LogOn successful'
} else {
    Write-Output 'VERIFY_STARTUP missing: GameServer.LogOn successful'
    `$failed = `$true
}
if (`$failed) { exit 4 }
"@
    Invoke-RemotePowerShell $script 'remote: verify newest dedicated log'
}

try {
    if (-not (Test-Path -LiteralPath $ModsRoot -PathType Container)) {
        throw "mods folder not found: $ModsRoot"
    }
    if ((-not $Mod -or $Mod.Count -eq 0) -and (-not $RemoveMod -or $RemoveMod.Count -eq 0)) {
        throw "At least one -Mod or -RemoveMod value is required."
    }

    Log '=== Waka Notebook Deploy ==='
    Log "MO2 mods       : $ModsRoot"
    Log "Remote host    : $RemoteHost"
    Log "Remote dedi    : $RemoteDediPath"
    Log "Remote Mods    : $RemoteModsPath"
    Log "Apply          : $Apply"
    Log "Restart        : $Restart"
    Log "Verify         : $Verify"
    Log ("Deploy/copy targets: {0}" -f ($(if ($Mod -and $Mod.Count -gt 0) { $Mod -join ', ' } else { '(none)' })))
    Log ("Remove targets : {0}" -f ($(if ($RemoveMod -and $RemoveMod.Count -gt 0) { $RemoveMod -join ', ' } else { '(none)' })))

    $resolved = New-Object System.Collections.Generic.List[object]
    $claimed = @{}
    foreach ($requested in @($Mod)) {
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
    if ($resolved.Count -gt 0) {
        Log '  Deploy/copy targets:' 'Cyan'
        foreach ($real in $resolved) {
            Log ("    remove then copy: {0}/{1}" -f $RemoteModsPath, $real.Name)
        }
    } else {
        Log '  Deploy/copy targets: (none)' 'DarkGray'
    }
    if ($RemoveMod -and $RemoveMod.Count -gt 0) {
        Log '  Remove targets:' 'Cyan'
        foreach ($name in $RemoveMod) {
            Log ("    remove only: {0}/{1}" -f $RemoteModsPath, $name)
        }
    } else {
        Log '  Remove targets: (none)' 'DarkGray'
    }
    if ($Restart) { Log '  restart: stop process, start WakaStartDediHeadless.ps1 via scheduled task' }
    if ($Verify) { Log '  verify: newest output_log_dedi__*.txt for copied target load, plus removed-folder absence' }

    if (-not $Apply) {
        Log 'DryRun only. No ssh, scp, remote delete, copy, restart, or verify was executed.' 'Yellow'
        Write-Host ''
        Write-Host '[DRY RUN] Add -Apply to change the notebook server.' -ForegroundColor Yellow
        return
    }

    $resolvedTargets = @($resolved.ToArray())

    if ($Restart) { Stop-RemoteServer }
    Remove-RemoteMods -Targets $resolvedTargets
    Remove-RemoteModNames -Names $RemoveMod
    foreach ($real in $resolved) {
        Invoke-ScpCopy $real.FullName
    }
    if ($Restart) { Start-RemoteServer }
    if ($Verify) {
        Verify-RemoteLog -Targets $resolvedTargets
        Verify-RemoteRemovedMods -Names $RemoveMod
    }

    Log 'Notebook deploy completed.' 'Green'
} catch {
    Log "ERROR: $($_.Exception.Message)" 'Red'
    throw
} finally {
    $logLines | Set-Content -Path $LogPath -Encoding UTF8
    Log "Log written: $LogPath"
}

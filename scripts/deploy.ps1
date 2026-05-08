# scripts/deploy.ps1
# Build any C# Waka mods that have a Source/*.csproj and copy the produced DLL
# alongside the matching ModInfo.xml inside the MO2 mods folder.
#
# Usage (from repo root):
#   pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "C:\Modding\MO2\mods"
#
# Assumptions:
# - The repo's `mods/<Name>/` matches an MO2 inner folder named exactly the same
#   (after stripping any leading "Z+_" container prefix). We resolve by
#   searching MO2 mods\* for an inner folder whose name equals <Name>.
# - 7DTD managed DLLs are referenced by each .csproj via $(GameInstall) or a
#   relative HintPath. Confirm your csproj points at a real 7DTD install.

[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Mo2ModsRoot,
    [string] $Configuration = "Release",
    [string[]] $Only = @()
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$repoMods = Join-Path $repoRoot "mods"

if (-not (Test-Path $Mo2ModsRoot)) {
    throw "MO2 mods root not found: $Mo2ModsRoot"
}

function Find-Mo2InnerFolder {
    param([string] $InnerName)
    Get-ChildItem $Mo2ModsRoot -Directory | ForEach-Object {
        $candidate = Join-Path $_.FullName $InnerName
        if (Test-Path $candidate) { return $candidate }
        # Some MO2 containers wrap the inner with the same name + Z prefix
        $altCandidate = Get-ChildItem $_.FullName -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match "^Z+_$InnerName$" } |
            Select-Object -First 1
        if ($altCandidate) { return $altCandidate.FullName }
    } | Where-Object { $_ } | Select-Object -First 1
}

Get-ChildItem $repoMods -Directory | ForEach-Object {
    $modName = $_.Name
    if ($Only.Count -gt 0 -and $modName -notin $Only) { return }

    $sourceDir = Join-Path $_.FullName "Source"
    $csproj = if (Test-Path $sourceDir) {
        Get-ChildItem $sourceDir -Filter "*.csproj" -ErrorAction SilentlyContinue | Select-Object -First 1
    } else { $null }

    $deployTarget = Find-Mo2InnerFolder -InnerName $modName
    if (-not $deployTarget) {
        Write-Warning "[$modName] no MO2 inner folder found under $Mo2ModsRoot — skipping"
        return
    }

    if ($csproj) {
        Write-Host "[$modName] building $($csproj.Name)..."
        & dotnet build $csproj.FullName -c $Configuration | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "[$modName] build failed (exit $LASTEXITCODE) — skipping deploy"
            return
        }
        $bin = Join-Path $sourceDir "bin/$Configuration"
        $dlls = Get-ChildItem $bin -Filter "*.dll" -ErrorAction SilentlyContinue
        foreach ($dll in $dlls) {
            Copy-Item $dll.FullName $deployTarget -Force
            Write-Host "[$modName] deployed $($dll.Name) -> $deployTarget"
        }
    } else {
        Write-Host "[$modName] no .csproj — XML/asset-only mod, deploying Config/ + ModInfo.xml"
        # XML-only mods: sync repo content into MO2 mod folder
        Copy-Item -Path (Join-Path $_.FullName "*") -Destination $deployTarget -Recurse -Force -Exclude @("Source", "bin", "obj", "*.user")
    }
}

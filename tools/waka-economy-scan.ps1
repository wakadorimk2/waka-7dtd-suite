param(
  [string]$Root = (Split-Path -Parent $PSScriptRoot),
  [string]$ProfilePath = (Join-Path (Split-Path -Parent $PSScriptRoot) "profiles\Default"),
  [string]$Out = (Join-Path (Split-Path -Parent $PSScriptRoot) "_analysis\waka-economy-risk-report.html")
)

$ErrorActionPreference = "Stop"

function Get-ModName {
  param([string]$Path)
  $relative = [System.IO.Path]::GetRelativePath($Root, $Path)
  $parts = $relative -split '[\\/]'
  if ($parts.Length -ge 2 -and $parts[0] -eq "mods") { return $parts[1] }
  return $parts[0]
}

function Get-EnabledRealMods {
  param([string]$ProfilePath)

  $modlistPath = Join-Path $ProfilePath "modlist.txt"
  $modsRoot = Join-Path $Root "mods"
  $realMods = New-Object System.Collections.Generic.List[object]
  $claimed = @{}

  if (-not (Test-Path $modlistPath)) {
    return @(Get-ChildItem -Path $modsRoot -Directory -ErrorAction SilentlyContinue | Where-Object {
      Test-Path (Join-Path $_.FullName "ModInfo.xml")
    })
  }

  foreach ($rawLine in Get-Content $modlistPath) {
    $line = $rawLine.Trim()
    if (-not $line.StartsWith("+")) { continue }
    $mo2Mod = $line.Substring(1)
    if (-not $mo2Mod -or $mo2Mod.EndsWith("_separator")) { continue }

    $mo2Path = Join-Path $modsRoot $mo2Mod
    if (-not (Test-Path $mo2Path)) { continue }

    $candidates = @()
    if (Test-Path (Join-Path $mo2Path "ModInfo.xml")) {
      $candidates = @(Get-Item $mo2Path)
    } else {
      $candidates = @(Get-ChildItem $mo2Path -Directory | Where-Object {
        Test-Path (Join-Path $_.FullName "ModInfo.xml")
      })
    }

    foreach ($candidate in $candidates) {
      if ($claimed.ContainsKey($candidate.Name)) { continue }
      $claimed[$candidate.Name] = $mo2Mod
      $realMods.Add([pscustomobject]@{
        Mo2Mod = $mo2Mod
        RealName = $candidate.Name
        FullName = $candidate.FullName
      })
    }
  }

  return @($realMods | Sort-Object RealName, FullName)
}

function Get-Attr {
  param([string]$Text, [string]$Name)
  $m = [regex]::Match($Text, "\b$([regex]::Escape($Name))\s*=\s*`"([^`"]*)`"")
  if ($m.Success) { return $m.Groups[1].Value }
  return $null
}

function To-Number {
  param([string]$Text)
  if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
  $first = ($Text -split ',')[0].Trim()
  $value = 0.0
  if ([double]::TryParse($first, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$value)) {
    return $value
  }
  return $null
}

function Test-QuestRewardPatch {
  param([string]$QuestId, [string]$XPath)

  if ($XPath -notmatch "/quests/quest" -or $XPath -notmatch "casinoCoin") { return $false }

  $directIds = @([regex]::Matches($XPath, "@id='([^']+)'") | ForEach-Object { $_.Groups[1].Value } | Where-Object { $_ -ne "casinoCoin" })
  if ($directIds.Count -gt 0) {
    return @($directIds | Where-Object { $_ -eq $QuestId }).Count -gt 0
  }

  $prefixes = @([regex]::Matches($XPath, "starts-with\(@id,'([^']+)'\)") | ForEach-Object { $_.Groups[1].Value })
  if ($prefixes.Count -gt 0 -and @($prefixes | Where-Object { $QuestId.StartsWith($_) }).Count -eq 0) {
    return $false
  }

  $contains = @([regex]::Matches($XPath, "contains\(@id,'([^']+)'\)") | ForEach-Object { $_.Groups[1].Value })
  if ($contains.Count -gt 0 -and @($contains | Where-Object { $QuestId.Contains($_) }).Count -eq 0) {
    return $false
  }

  return $true
}

function New-Finding {
  param(
    [string]$Kind,
    [string]$Name,
    [string]$Mod,
    [string]$File,
    [int]$Line,
    [double]$Score,
    [string]$Reason,
    [hashtable]$Data
  )
  [pscustomobject]@{
    kind = $Kind
    name = $Name
    mod = $Mod
    file = [System.IO.Path]::GetRelativePath($Root, $File)
    line = $Line
    score = [math]::Round($Score, 2)
    reason = $Reason
    data = $Data
  }
}

$realMods = Get-EnabledRealMods -ProfilePath $ProfilePath
$xmlFiles = @(
  foreach ($realMod in $realMods) {
    Get-ChildItem -Path $realMod.FullName -Recurse -File -Include *.xml -ErrorAction SilentlyContinue | Sort-Object FullName
  }
)
$items = @{}
$findings = New-Object System.Collections.Generic.List[object]
$recipes = New-Object System.Collections.ArrayList
$patchOps = New-Object System.Collections.Generic.List[object]
$tradeFindings = New-Object System.Collections.Generic.List[object]
$questRewards = New-Object System.Collections.Generic.List[object]
$questPatchOps = New-Object System.Collections.Generic.List[object]
$patchSeq = 0

foreach ($file in $xmlFiles) {
  $text = Get-Content $file.FullName -Raw
  $mod = Get-ModName $file.FullName

  foreach ($m in [regex]::Matches($text, "(?s)<(item|block)\s+[^>]*\bname=`"([^`"]+)`"[^>]*>.*?</\1>")) {
    $kind = $m.Groups[1].Value
    $name = $m.Groups[2].Value
    $body = $m.Value
    $prefix = $text.Substring(0, $m.Index)
    $line = 1 + ([regex]::Matches($prefix, "`n")).Count

    $economic = $null
    $stack = $null
    $extends = $null
    $template = $null
    $tags = ""
    $hidden = $false

    foreach ($p in [regex]::Matches($body, "<property\s+[^>]*>")) {
      $prop = $p.Value
      $pname = Get-Attr $prop "name"
      $pvalue = Get-Attr $prop "value"
      if ($pname -eq "EconomicValue") { $economic = To-Number $pvalue }
      elseif ($pname -eq "Stacknumber") { $stack = To-Number $pvalue }
      elseif ($pname -eq "Extends") { $extends = $pvalue }
      elseif ($pname -eq "TraderStageTemplate") { $template = $pvalue }
      elseif ($pname -eq "Tags") { $tags = $pvalue }
      elseif ($pname -eq "Hidden") { $hidden = ($pvalue -match "^(true|1)$") }
    }

    if ($null -eq $stack -or $stack -lt 1) { $stack = 1 }
    $record = [pscustomobject]@{
      kind = $kind
      name = $name
      mod = $mod
      file = [System.IO.Path]::GetRelativePath($Root, $file.FullName)
      line = $line
      economicValue = $economic
      rawEconomicValue = $economic
      stack = [int]$stack
      rawStack = [int]$stack
      stackValue = if ($null -ne $economic) { [double]$economic * [double]$stack } else { $null }
      extends = $extends
      traderStageTemplate = $template
      tags = $tags
      sellable = $true
      hidden = $hidden
      patchCount = 0
    }
    $items[$name] = $record

    if ($false -and $null -ne $economic) {
      $score = $economic
      $reasons = New-Object System.Collections.Generic.List[string]
      if ($economic -ge 5000) { $reasons.Add("very high explicit EconomicValue") }
      elseif ($economic -ge 1000) { $reasons.Add("high explicit EconomicValue") }
      if ($stack -gt 1 -and ($economic * $stack) -ge 10000) {
        $score += [math]::Min(($economic * $stack) / 10.0, 20000)
        $reasons.Add("large stack value")
      }
      if ($name -match "(crop|seed|corn|grace|farm|planted)" -and $economic -ge 20) {
        $score += 2000
        $reasons.Add("farmable or crop-adjacent value")
      }
      if ($name -match "(bundle|ammo|round|shell|bullet)" -and $economic -ge 1000) {
        $score += 1000
        $reasons.Add("ammo or bundle value")
      }
      if ($reasons.Count -gt 0) {
        $findings.Add((New-Finding -Kind "value" -Name $name -Mod $mod -File $file.FullName -Line $line -Score $score -Reason ($reasons -join "; ") -Data @{
          economicValue = $economic
          stack = [int]$stack
          stackValue = [math]::Round($economic * $stack, 2)
          template = $template
          extends = $extends
        }))
      }
    }
  }

  foreach ($m in [regex]::Matches($text, "(?s)<set\s+[^>]*xpath=`"([^`"]*property\[@name='(EconomicValue|Stacknumber|SellableToTrader|Hidden)'\]/@value[^`"]*)`"[^>]*>\s*([^<]+)\s*</set>")) {
    $xpath = $m.Groups[1].Value
    $propertyName = $m.Groups[2].Value
    $rawValue = $m.Groups[3].Value.Trim()
    $name = "unknown"
    $nameMatch = [regex]::Match($xpath, "@name='([^']+)'")
    if ($nameMatch.Success) { $name = $nameMatch.Groups[1].Value }
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    $patchOps.Add([pscustomobject]@{
      seq = $patchSeq++
      name = $name
      property = $propertyName
      value = $rawValue
      mod = $mod
      file = $file.FullName
      line = $line
      method = "set"
      xpath = $xpath
    })
  }

  foreach ($m in [regex]::Matches($text, "(?s)<set\s+[^>]*xpath=`"([^`"]*/quests/quest[^`"]*reward[^`"]*@id='casinoCoin'[^`"]*/@value)`"[^>]*>\s*([^<]+)\s*</set>")) {
    $xpath = $m.Groups[1].Value
    $rawValue = $m.Groups[2].Value.Trim()
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    $value = To-Number $rawValue
    if ($null -ne $value) {
      $questPatchOps.Add([pscustomobject]@{
        seq = $patchSeq++
        value = [double]$value
        mod = $mod
        file = $file.FullName
        line = $line
        xpath = $xpath
      })
    }
  }

  foreach ($m in [regex]::Matches($text, "(?s)<setattribute\s+[^>]*xpath=`"([^`"]*item\[@name='[^']+'\]/property\[@name='(EconomicValue|Stacknumber|SellableToTrader|Hidden)'\][^`"]*)`"[^>]*name=`"value`"[^>]*>\s*([^<]+)\s*</setattribute>")) {
    $xpath = $m.Groups[1].Value
    $propertyName = $m.Groups[2].Value
    $rawValue = $m.Groups[3].Value.Trim()
    $name = "unknown"
    $nameMatch = [regex]::Match($xpath, "item\[@name='([^']+)'\]")
    if ($nameMatch.Success) { $name = $nameMatch.Groups[1].Value }
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    $patchOps.Add([pscustomobject]@{
      seq = $patchSeq++
      name = $name
      property = $propertyName
      value = $rawValue
      mod = $mod
      file = $file.FullName
      line = $line
      method = "setattribute"
      xpath = $xpath
    })
  }

  foreach ($m in [regex]::Matches($text, "(?s)<append\s+[^>]*xpath=`"([^`"]*(?:item|block)\[@name='([^']+)'\][^`"]*)`"[^>]*>.*?</append>")) {
    $xpath = $m.Groups[1].Value
    $name = $m.Groups[2].Value
    $body = $m.Value
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    foreach ($p in [regex]::Matches($body, "<property\s+[^>]*>")) {
      $propertyName = Get-Attr $p.Value "name"
      if ($propertyName -notin @("EconomicValue", "Stacknumber", "SellableToTrader", "Hidden")) { continue }
      $rawValue = Get-Attr $p.Value "value"
      if ($null -eq $rawValue) { continue }
      $patchOps.Add([pscustomobject]@{
        seq = $patchSeq++
        name = $name
        property = $propertyName
        value = $rawValue
        mod = $mod
        file = $file.FullName
        line = $line
        method = "append"
        xpath = $xpath
      })
    }
  }

  foreach ($m in [regex]::Matches($text, "(?s)<recipe\s+[^>]*\bname=`"([^`"]+)`"[^>]*>.*?</recipe>")) {
    $recipe = $m.Value
    $name = $m.Groups[1].Value
    $count = To-Number (Get-Attr $recipe "count")
    if ($null -eq $count) { $count = 1 }
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    $outputValue = $null
    if ($items.ContainsKey($name) -and $null -ne $items[$name].economicValue) {
      $outputValue = [double]$items[$name].economicValue * [double]$count
    }
    $ingredientNames = New-Object System.Collections.Generic.List[string]
    $ingredientRecords = New-Object System.Collections.Generic.List[object]
    $knownInputValue = 0.0
    $unknownInputs = 0
    foreach ($im in [regex]::Matches($recipe, "<ingredient\s+[^>]*\bname=`"([^`"]+)`"[^>]*>")) {
      $iname = $im.Groups[1].Value
      $icount = To-Number (Get-Attr $im.Value "count")
      if ($null -eq $icount) { $icount = 1 }
      $ingredientNames.Add("$iname x$icount")
      $ingredientRecords.Add([pscustomobject]@{ name = $iname; count = [double]$icount })
      if ($items.ContainsKey($iname) -and $null -ne $items[$iname].economicValue) {
        $knownInputValue += [double]$items[$iname].economicValue * [double]$icount
      } else {
        $unknownInputs++
      }
    }

    $isFarm = $name -match "(seed|crop|corn|grace|farm|planted)" -or ($ingredientNames -join " ") -match "(seed|crop|corn|grace|farm|planted)"
    [void]$recipes.Add([pscustomobject]@{
      name = $name
      mod = $mod
      file = $file.FullName
      line = $line
      count = [double]$count
      ingredientsText = $ingredientNames -join ", "
      ingredients = @($ingredientRecords.ToArray())
      isFarm = $isFarm
    })
    if ($false -and (($null -ne $outputValue -and $outputValue -ge 1000) -or $isFarm)) {
      $margin = if ($null -ne $outputValue) { $outputValue - $knownInputValue } else { 0 }
      $score = [math]::Max(0, $margin) + $(if ($isFarm) { 2500 } else { 0 }) + $(if ($unknownInputs -gt 0) { 250 } else { 0 })
      $findings.Add((New-Finding -Kind "recipe" -Name $name -Mod $mod -File $file.FullName -Line $line -Score $score -Reason "recipe output/input economy candidate" -Data @{
        outputCount = [int]$count
        outputValue = if ($null -ne $outputValue) { [math]::Round($outputValue, 2) } else { $null }
        knownInputValue = [math]::Round($knownInputValue, 2)
        knownMargin = if ($null -ne $outputValue) { [math]::Round($margin, 2) } else { $null }
        unknownInputs = $unknownInputs
        ingredients = $ingredientNames -join ", "
      }))
    }
  }

  foreach ($m in [regex]::Matches($text, "(?s)<quest\s+[^>]*\bid=`"([^`"]+)`"[^>]*>.*?</quest>")) {
    $questId = $m.Groups[1].Value
    $questBody = $m.Value
    foreach ($rm in [regex]::Matches($questBody, "<reward\s+[^>]*\btype=`"Item`"[^>]*\bid=`"casinoCoin`"[^>]*>")) {
      $value = To-Number (Get-Attr $rm.Value "value")
      if ($null -eq $value) { continue }
      $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index + $rm.Index), "`n")).Count
      $questRewards.Add([pscustomobject]@{
        questId = $questId
        rawValue = [double]$value
        effectiveValue = [double]$value
        mod = $mod
        file = $file.FullName
        line = $line
        patchCount = 0
        patchSource = $null
      })
    }
  }

  foreach ($m in [regex]::Matches($text, "(?im)^.*(Duke|duke|BarteringSelling|BarteringBuying).*$")) {
    $lineText = $m.Value.Trim()
    $line = 1 + ([regex]::Matches($text.Substring(0, $m.Index), "`n")).Count
    $numbers = [regex]::Matches($lineText, "-?\d+(?:\.\d+)?") | ForEach-Object { [double]::Parse($_.Value, [System.Globalization.CultureInfo]::InvariantCulture) }
    $max = ($numbers | Measure-Object -Maximum).Maximum
    if ($lineText -match "BarteringSelling|BarteringBuying|Duke|duke") {
      $score = 500 + $(if ($null -ne $max) { [math]::Min($max * 5, 10000) } else { 0 })
      if ($lineText -match "BarteringSelling") { $score += 1500 }
      $tradeFindings.Add((New-Finding -Kind "trade" -Name "line $line" -Mod $mod -File $file.FullName -Line $line -Score $score -Reason "trade currency or barter modifier line" -Data @{
        text = $lineText
      }))
    }
  }
}

$appliedPatchCount = 0
foreach ($op in ($patchOps | Sort-Object seq)) {
  if (-not $items.ContainsKey($op.name)) { continue }
  $item = $items[$op.name]
  switch ($op.property) {
    "EconomicValue" {
      $value = To-Number $op.value
      if ($null -ne $value) {
        $item.economicValue = [double]$value
        $item.patchCount++
        $appliedPatchCount++
      }
    }
    "Stacknumber" {
      $value = To-Number $op.value
      if ($null -ne $value -and $value -ge 1) {
        $item.stack = [int]$value
        $item.patchCount++
        $appliedPatchCount++
      }
    }
    "SellableToTrader" {
      $item.sellable = ($op.value -notmatch "^(false|0)$")
      $item.patchCount++
      $appliedPatchCount++
    }
    "Hidden" {
      $item.hidden = ($op.value -match "^(true|1)$")
      $item.patchCount++
      $appliedPatchCount++
    }
  }
  if ($null -ne $item.economicValue) {
    $item.stackValue = [double]$item.economicValue * [double]$item.stack
  }
}

$findings = New-Object System.Collections.Generic.List[object]
foreach ($item in $items.Values) {
  if ($null -eq $item.economicValue -or -not $item.sellable -or $item.hidden) { continue }
  $economic = [double]$item.economicValue
  $stack = [int]$item.stack
  $score = $economic
  $reasons = New-Object System.Collections.Generic.List[string]
  if ($economic -ge 5000) { $reasons.Add("high effective EconomicValue") }
  elseif ($economic -ge 1000) { $reasons.Add("moderate effective EconomicValue") }
  if ($stack -gt 1 -and ($economic * $stack) -ge 10000) {
    $score += [math]::Min(($economic * $stack) / 10.0, 20000)
    $reasons.Add("large effective stack value")
  }
  if ($item.name -match "(crop|seed|corn|grace|farm|planted)" -and $economic -ge 20) {
    $score += 2000
    $reasons.Add("farmable or crop-adjacent value")
  }
  if ($item.name -match "(bundle|ammo|round|shell|bullet)" -and $economic -ge 1000) {
    $score += 1000
    $reasons.Add("ammo or bundle value")
  }
  if ($reasons.Count -gt 0) {
    $findings.Add((New-Finding -Kind "value" -Name $item.name -Mod $item.mod -File (Join-Path $Root $item.file) -Line $item.line -Score $score -Reason ($reasons -join "; ") -Data @{
      economicValue = $economic
      rawEconomicValue = $item.rawEconomicValue
      stack = $stack
      rawStack = $item.rawStack
      stackValue = [math]::Round($economic * $stack, 2)
      sellable = $item.sellable
      hidden = $item.hidden
      patchCount = $item.patchCount
      template = $item.traderStageTemplate
      extends = $item.extends
    }))
  }
}

foreach ($recipe in $recipes) {
  if (-not $items.ContainsKey($recipe.name)) { continue }
  $output = $items[$recipe.name]
  if ($null -eq $output.economicValue -or -not $output.sellable -or $output.hidden) { continue }
  $outputValue = [double]$output.economicValue * [double]$recipe.count
  $knownInputValue = 0.0
  $unknownInputs = 0
  foreach ($ingredient in $recipe.ingredients) {
    if ($items.ContainsKey($ingredient.name) -and $null -ne $items[$ingredient.name].economicValue) {
      $knownInputValue += [double]$items[$ingredient.name].economicValue * [double]$ingredient.count
    } else {
      $unknownInputs++
    }
  }
  if (($outputValue -ge 1000) -or $recipe.isFarm) {
    $margin = $outputValue - $knownInputValue
    $score = [math]::Max(0, $margin) + $(if ($recipe.isFarm) { 2500 } else { 0 }) + $(if ($unknownInputs -gt 0) { 250 } else { 0 })
    if ($score -le 0) { continue }
    $findings.Add((New-Finding -Kind "recipe" -Name $recipe.name -Mod $recipe.mod -File $recipe.file -Line $recipe.line -Score $score -Reason "effective recipe output/input economy candidate" -Data @{
      outputCount = [int]$recipe.count
      outputValue = [math]::Round($outputValue, 2)
      knownInputValue = [math]::Round($knownInputValue, 2)
      knownMargin = [math]::Round($margin, 2)
      unknownInputs = $unknownInputs
      ingredients = $recipe.ingredientsText
      outputSellable = $output.sellable
      outputHidden = $output.hidden
      outputPatchCount = $output.patchCount
    }))
  }
}

foreach ($reward in $questRewards) {
  foreach ($op in ($questPatchOps | Sort-Object seq)) {
    if (Test-QuestRewardPatch -QuestId $reward.questId -XPath $op.xpath) {
      $reward.effectiveValue = [double]$op.value
      $reward.patchCount++
      $reward.patchSource = "$($op.mod):$($op.line)"
    }
  }

  $rawValue = [double]$reward.rawValue
  $effectiveValue = [double]$reward.effectiveValue
  if ($rawValue -lt 5000 -and $effectiveValue -lt 2500 -and $reward.patchCount -eq 0) { continue }

  $score = 500 + [math]::Min($effectiveValue * 2.0, 12000)
  if ($rawValue -gt $effectiveValue) { $score += [math]::Min(($rawValue - $effectiveValue) / 5.0, 1000) }
  $findings.Add((New-Finding -Kind "reward" -Name $reward.questId -Mod $reward.mod -File $reward.file -Line $reward.line -Score $score -Reason "effective quest casinoCoin reward" -Data @{
    rawValue = [math]::Round($rawValue, 2)
    effectiveValue = [math]::Round($effectiveValue, 2)
    patchCount = $reward.patchCount
    patchSource = $reward.patchSource
  }))
}

foreach ($finding in $tradeFindings) {
  $findings.Add($finding)
}

$topFindings = $findings | Sort-Object score -Descending | Select-Object -First 1000
$topItems = $items.Values | Where-Object { $null -ne $_.economicValue -and $_.sellable -and -not $_.hidden } | Sort-Object economicValue -Descending | Select-Object -First 300
$payload = [pscustomobject]@{
  generatedAt = (Get-Date).ToString("s")
  root = $Root
  fileCount = $xmlFiles.Count
  itemCount = $items.Count
  patchOpCount = $patchOps.Count
  questPatchOpCount = $questPatchOps.Count
  appliedPatchCount = $appliedPatchCount
  findings = @($topFindings)
  items = @($topItems)
}
$json = $payload | ConvertTo-Json -Depth 8 -Compress
$json = $json -replace '</script>', '<\/script>'

$html = @'
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Waka Economy Risk Report</title>
  <style>
    :root { --bg:#f5f6f8; --panel:#fff; --ink:#1f2328; --muted:#656d76; --line:#d0d7de; --hot:#cf222e; --warn:#bf8700; --ok:#1a7f37; --accent:#0969da; }
    * { box-sizing:border-box; }
    body { margin:0; font-family:"Segoe UI",system-ui,sans-serif; background:var(--bg); color:var(--ink); }
    main { width:min(1400px, calc(100vw - 32px)); margin:0 auto; padding:22px 0 34px; }
    h1 { margin:0; font-size:24px; line-height:1.2; }
    .sub { color:var(--muted); font-size:13px; margin-top:6px; }
    .metrics { display:grid; grid-template-columns:repeat(4,minmax(120px,1fr)); gap:10px; margin:16px 0; }
    .metric,.panel { background:var(--panel); border:1px solid var(--line); border-radius:8px; box-shadow:0 1px 2px rgba(31,35,40,.08); }
    .metric { padding:12px 14px; }
    .label { color:var(--muted); font-size:12px; margin-bottom:5px; }
    .value { font-size:22px; font-weight:650; }
    .grid { display:grid; grid-template-columns:minmax(0,1fr) 380px; gap:14px; align-items:start; }
    .panel { padding:14px; }
    h2 { margin:0 0 10px; font-size:15px; }
    .toolbar { display:flex; gap:8px; flex-wrap:wrap; margin-bottom:10px; }
    input,select { border:1px solid var(--line); border-radius:6px; min-height:34px; padding:7px 9px; font:inherit; background:#fff; color:var(--ink); }
    input[type=search] { flex:1 1 260px; }
    table { width:100%; border-collapse:collapse; font-size:13px; }
    th,td { padding:7px 8px; border-bottom:1px solid #eaeef2; vertical-align:top; }
    th { position:sticky; top:0; z-index:1; background:#f6f8fa; color:var(--muted); text-align:left; }
    td.num,th.num { text-align:right; white-space:nowrap; }
    .table-wrap { max-height:650px; overflow:auto; border:1px solid var(--line); border-radius:6px; }
    .pill { display:inline-flex; align-items:center; min-height:22px; padding:2px 7px; border-radius:999px; background:#eef2f6; color:#24292f; font-size:12px; white-space:nowrap; }
    .hot { color:var(--hot); font-weight:650; }
    .warn { color:var(--warn); font-weight:650; }
    .muted { color:var(--muted); }
    .chart { height:320px; border:1px solid var(--line); border-radius:6px; background:#fff; overflow:hidden; }
    canvas { width:100%; height:100%; display:block; }
    .note { font-size:12px; line-height:1.45; color:var(--muted); }
    @media (max-width: 1000px) { .grid,.metrics { display:block; } .metric,.panel { margin-bottom:10px; } }
  </style>
</head>
<body>
<main>
  <h1>Waka Economy Risk Report</h1>
  <div class="sub" id="summary"></div>

  <section class="metrics">
    <div class="metric"><div class="label">Findings</div><div class="value" id="m-findings">0</div></div>
    <div class="metric"><div class="label">Priced items</div><div class="value" id="m-items">0</div></div>
    <div class="metric"><div class="label">Highest value</div><div class="value" id="m-highest">0</div></div>
    <div class="metric"><div class="label">Applied patches</div><div class="value" id="m-patches">0</div></div>
  </section>

  <section class="grid">
    <section class="panel">
      <h2>Risk Candidates</h2>
      <div class="toolbar">
        <input id="q" type="search" placeholder="Filter by item, mod, reason, file">
        <select id="kind">
          <option value="">All kinds</option>
          <option value="value">Value</option>
          <option value="recipe">Recipe</option>
          <option value="patch">Patch</option>
          <option value="trade">Trade</option>
          <option value="reward">Reward</option>
        </select>
        <select id="category">
          <option value="">All categories</option>
          <option value="weapons">Weapons</option>
          <option value="ammo">Ammo</option>
          <option value="food-farming">Food/Farming</option>
          <option value="materials">Materials</option>
          <option value="tokens">Tokens</option>
          <option value="medical">Medical/Drugs</option>
          <option value="tools-armor">Tools/Armor</option>
          <option value="trade">Trade</option>
          <option value="other">Other</option>
        </select>
      </div>
      <div class="table-wrap">
        <table>
          <thead><tr><th>Risk</th><th>Kind</th><th>Category</th><th>Name</th><th>Reason</th><th>Details</th><th>Source</th></tr></thead>
          <tbody id="rows"></tbody>
        </table>
      </div>
    </section>

    <aside class="panel">
      <h2>Effective EconomicValue</h2>
      <div class="chart"><canvas id="chart"></canvas></div>
      <p class="note">Scatter uses estimated effective values after applying EconomicValue, Stacknumber, SellableToTrader, and Hidden XML patches found in the enabled mod folders. It is still a static audit, not the game engine's full XML merge.</p>
      <h2>Top Sellable Items</h2>
      <div class="table-wrap" style="max-height:280px">
        <table><thead><tr><th>Item</th><th class="num">Value</th><th class="num">Stack</th></tr></thead><tbody id="items"></tbody></table>
      </div>
    </aside>
  </section>
</main>
<script id="payload" type="application/json">__PAYLOAD_JSON__</script>
<script>
const data = JSON.parse(document.getElementById("payload").textContent);
const rows = document.getElementById("rows");
const items = document.getElementById("items");
const q = document.getElementById("q");
const kind = document.getElementById("kind");
const category = document.getElementById("category");

function fmt(n) {
  if (n === null || n === undefined || Number.isNaN(Number(n))) return "";
  return Number(n).toLocaleString(undefined, { maximumFractionDigits: 2 });
}
function riskClass(score) {
  if (score >= 10000) return "hot";
  if (score >= 3000) return "warn";
  return "";
}
function details(f) {
  const d = f.data || {};
  if (f.kind === "value") return `value ${fmt(d.economicValue)}, stack ${fmt(d.stack)}, stack value ${fmt(d.stackValue)}`;
  if (f.kind === "recipe") return `out ${fmt(d.outputValue)}, inputs ${fmt(d.knownInputValue)}, margin ${fmt(d.knownMargin)}; ${d.ingredients || ""}`;
  if (f.kind === "patch") return `value ${fmt(d.value)}; ${d.xpath || ""}`;
  if (f.kind === "trade") return d.text || "";
  if (f.kind === "reward") return `raw ${fmt(d.rawValue)}, effective ${fmt(d.effectiveValue)}, patches ${fmt(d.patchCount)} ${d.patchSource || ""}`;
  return JSON.stringify(d);
}
function classify(text, kindValue) {
  const s = String(text || "").toLowerCase();
  if (kindValue === "trade" || kindValue === "reward" || /bartering|duke|casino/.test(s)) return "trade";
  if (/affix|token|ticket|badge|currency/.test(s)) return "tokens";
  if (/ammo|bullet|shell|round|arrow|bolt|rocket|grenade|molotov|bundle/.test(s)) return "ammo";
  if (/gun|rifle|pistol|shotgun|smg|weapon|machete|spear|club|bat|knife|bow|crossbow|turret|minigun|flamethrower/.test(s)) return "weapons";
  if (/food|drink|liquor|crop|seed|corn|grace|farm|planted|stew|bread|meal|sugar|potato|yucca|honey|beer|moonshine|vodka/.test(s)) return "food-farming";
  if (/medical|drug|antibiotic|vitamin|steroid|bandage|firstaid|splint|painkiller|health/.test(s)) return "medical";
  if (/armor|helmet|gloves|boots|outfit|tool|axe|pickaxe|wrench|ratchet|auger|chainsaw/.test(s)) return "tools-armor";
  if (/resource|parts|steel|iron|brass|lead|copper|powder|nugget|canister|shell|kit|acid|oil|cloth|leather|polymer|spring/.test(s)) return "materials";
  return "other";
}
function renderRows() {
  const term = q.value.trim().toLowerCase();
  const k = kind.value;
  const c = category.value;
  rows.innerHTML = "";
  const filtered = data.findings.filter(f => {
    if (k && f.kind !== k) return false;
    const cat = classify(`${f.name} ${f.reason} ${details(f)} ${f.mod}`, f.kind);
    if (c && cat !== c) return false;
    if (!term) return true;
    return `${f.name} ${f.mod} ${f.reason} ${f.file} ${details(f)}`.toLowerCase().includes(term);
  });
  for (const f of filtered) {
    const cat = classify(`${f.name} ${f.reason} ${details(f)} ${f.mod}`, f.kind);
    const tr = document.createElement("tr");
    tr.innerHTML = `<td class="num ${riskClass(f.score)}">${fmt(f.score)}</td><td><span class="pill">${f.kind}</span></td><td><span class="pill">${cat}</span></td><td>${f.name}</td><td>${f.reason}</td><td class="muted">${details(f)}</td><td class="muted">${f.file}:${f.line}<br>${f.mod}</td>`;
    rows.appendChild(tr);
  }
}
function renderItems() {
  items.innerHTML = "";
  for (const it of data.items.slice(0, 80)) {
    const tr = document.createElement("tr");
    tr.innerHTML = `<td>${it.name}<br><span class="muted">${it.mod}</span></td><td class="num">${fmt(it.economicValue)}</td><td class="num">${fmt(it.stack)}</td>`;
    items.appendChild(tr);
  }
}
function drawChart() {
  const canvas = document.getElementById("chart");
  const ctx = canvas.getContext("2d");
  const rect = canvas.getBoundingClientRect();
  const dpr = window.devicePixelRatio || 1;
  canvas.width = Math.max(1, Math.round(rect.width * dpr));
  canvas.height = Math.max(1, Math.round(rect.height * dpr));
  ctx.setTransform(dpr,0,0,dpr,0,0);
  const w = rect.width, h = rect.height, l = 46, r = 12, t = 14, b = 32;
  const plotW = w - l - r, plotH = h - t - b;
  ctx.clearRect(0,0,w,h);
  ctx.fillStyle = "#fff"; ctx.fillRect(0,0,w,h);
  const maxVal = Math.max(1, ...data.items.map(x => Number(x.economicValue) || 0));
  const maxStack = Math.max(1, ...data.items.map(x => Number(x.stack) || 1));
  ctx.strokeStyle = "#d0d7de"; ctx.strokeRect(l,t,plotW,plotH);
  ctx.fillStyle = "#656d76"; ctx.font = "12px Segoe UI"; ctx.textAlign = "right";
  ctx.fillText(fmt(maxVal), l - 6, t + 8);
  ctx.fillText("0", l - 6, t + plotH);
  ctx.textAlign = "center"; ctx.fillText("stack", l + plotW / 2, h - 16);
  for (const it of data.items) {
    const val = Number(it.economicValue) || 0;
    const stack = Number(it.stack) || 1;
    const x = l + Math.log10(stack) / Math.log10(maxStack) * plotW;
    const y = t + (1 - Math.log10(val + 1) / Math.log10(maxVal + 1)) * plotH;
    ctx.fillStyle = /crop|seed|corn|farm|planted/i.test(it.name) ? "#cf222e" : "#0969da";
    ctx.globalAlpha = .62;
    ctx.beginPath(); ctx.arc(x, y, 3.2, 0, Math.PI * 2); ctx.fill();
  }
  ctx.globalAlpha = 1;
}
document.getElementById("summary").textContent = `Generated ${data.generatedAt} from ${data.root}`;
document.getElementById("m-findings").textContent = fmt(data.findings.length);
document.getElementById("m-items").textContent = fmt(data.itemCount);
document.getElementById("m-patches").textContent = fmt(data.appliedPatchCount);
document.getElementById("m-highest").textContent = fmt(Math.max(0, ...data.items.map(x => Number(x.economicValue) || 0)));
q.addEventListener("input", renderRows);
kind.addEventListener("change", renderRows);
category.addEventListener("change", renderRows);
window.addEventListener("resize", drawChart);
renderRows(); renderItems(); drawChart();
</script>
</body>
</html>
'@

$html = $html.Replace("__PAYLOAD_JSON__", $json)

$outDir = Split-Path -Parent $Out
if (-not (Test-Path $outDir)) {
  New-Item -ItemType Directory -Path $outDir | Out-Null
}
Set-Content -Path $Out -Value $html -Encoding UTF8
Write-Output "Wrote $Out"
Write-Output "Findings: $($topFindings.Count), priced items: $($topItems.Count), XML files: $($xmlFiles.Count)"

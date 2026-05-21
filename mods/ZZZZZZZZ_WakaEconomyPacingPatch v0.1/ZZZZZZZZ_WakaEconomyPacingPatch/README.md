# Waka Economy Pacing Patch

## Purpose

Reduce major economy acceleration points without flattening every reward or item value.

## Changes

- Reduces Super Corn resale pressure.
- Slows Super Corn seed expansion slightly.
- Halves Better Barter sell-side scaling.
- Moderates quest Dukes and reward-choice inflation.
- Does not own ammo pricing or ammo loot flow.

## Dependencies

- Current active profile economy stack.
- Better Barter progression from the loaded perk mods.
- Quest reward mods such as FNS reward changes and quest progression patches.

## Validation

- Rerun `tools\waka-economy-scan.ps1` after economy XML edits.
- Compare `_analysis\waka-economy-risk-report.html` after each pass.
- Confirm the latest client or dedicated log has no economy-patch `XPath`, `XML`, or `ModInfo` errors.

## Safety Notes

- Use `profiles\Default\modlist.txt` as the scan boundary; inactive XML can mislead economy audits.
- Quest reward pressure may remain even after item resale fixes.

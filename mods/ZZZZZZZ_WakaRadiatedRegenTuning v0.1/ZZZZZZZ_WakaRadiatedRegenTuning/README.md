# Waka Radiated Regen Tuning

## Purpose

Reduce radiated zombie regeneration while preserving tier differences across the current entity stack.

## Changes

- Patches `Config\entityclasses.xml`.
- Uses exact entity-name targets for radiated variants.
- Lowers regeneration values without replacing the shared buff model globally.

## Dependencies

- Current vanilla, EZS, and Bloodfall entity names.
- `WakaTierCurve` can affect validation because low-GameStage tests may swap high-tier spawns down before observation.

## Validation

- Confirm `ZZZZZZZ_WakaRadiatedRegenTuning` loads in the latest client log.
- Scan for `RadiatedRegen`, `XPath`, `XML`, and the mod name.
- Test in a GameStage context that actually preserves the radiated tier being evaluated.

## Safety Notes

- Do not broaden this into a shared buff patch unless the tier differences are intentionally being removed.
- Re-check exact entity names after entity-pack updates.

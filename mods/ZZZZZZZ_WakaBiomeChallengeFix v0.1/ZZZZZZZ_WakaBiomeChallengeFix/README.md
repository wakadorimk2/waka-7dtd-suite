# Waka Biome Challenge Fix

## Purpose

Make biome kill challenges count EZS tier variants spawned by `WakaTierCurve`.

## Changes

- Patches `Config\challenges.xml`.
- Extends the `entity_names` lists for burnt, desert, snow, and wasteland biome kill objectives.
- Covers EZS T2-T5 variants of biome-iconic zombies that vanilla challenge lists miss.

## Dependencies

- `ExtraSlayerChallenges` or the active challenge stack using the same biome objective names.
- `WakaTierCurve`, because it can swap vanilla biome zombies into EZS tier variants.
- EZS entity naming must match the patch lists.

## Validation

- Confirm `ZZZZZZZ_WakaBiomeChallengeFix` loads in the latest client log.
- Scan logs for challenge XML or XPath failures.
- In game, test a biome-iconic zombie variant and confirm the relevant biome challenge advances.

## Safety Notes

- This patch is intentionally exact-name based.
- Rebuild the entity list if EZS, Bloodfall, or challenge mods update their names.

# Waka Tier Curve

## Purpose

Unify vanilla, Enhanced Zombie Scaling, and Bloodfall zombie variants into a single tier curve driven by player GameStage.

## Changes

- Hooks `EntityFactory.CreateEntity(EntityCreationData)` so entity creation flows through one tier-selection point.
- Maps vanilla T1, EZS T2-T5, and Bloodfall higher tiers when matching variants exist.
- Covers world spawns, cached agents, and persistent entities restored through entity creation.
- Logs available tiers before choosing a replacement.

## Dependencies

- `Enhanced Zombie Scaling (2.5 Compatible)`
- `Bloodfall - Hardcore Overhaul`
- Current entity names in the loaded XML stack.
- EAC must be off because this is a Harmony DLL patch.

## Validation

- Confirm `ZZZZZ_WakaTierCurve` loads in the latest client or dedicated log.
- Use logs to validate replacement behavior; low-GameStage worlds can intentionally down-tier spawns.
- When testing high-tier tuning, use a GameStage context where the desired tier can survive the tier curve.

## Safety Notes

- Exact entity names matter. Do not assume Bloodfall or EZS variants exist without checking the loaded entity list.
- This mod can affect manual spawn tests because `se`-spawned entities also pass through entity creation.

# Waka CATUI Storm Timer Fix

## Purpose

Stop CATUI storm timer bindings from displaying negative remaining time after Sleep time skips.

## Changes

- Harmony patch for CATUI storm timer binding behavior.
- Clamps expired storm remaining time instead of letting the UI show negative values.
- Includes a world-time helper patch used by the timer calculation.

## Dependencies

- CATUI for 7DTD 2.5.
- Sleep/time-skip behavior that can move world time past storm expiry.
- EAC must be off because this is a Harmony DLL patch.

## Validation

- Build output should load as `ZZZZZZZZ_WakaCatuiStormTimerFix`.
- In the latest client log, check for loaded-mod lines and absence of CATUI storm timer exceptions.
- In game, perform or observe a time skip and confirm the storm timer does not go negative.

## Safety Notes

- Keep this focused on display/binding behavior; do not change weather scheduling here.
- Re-check CATUI class names after CATUI updates.

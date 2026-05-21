# Waka EFTX Audio Source Fix

## Purpose

Fix stale EFTX Armored Zs sound prefab paths for the current 7DTD 2.5 stack.

## Changes

- Patches `Config\sounds.xml`.
- Targets EFTX Armored Zs sound definitions that reference outdated `AudioSource` prefab paths.
- Keeps the change as an XML compatibility patch with no DLL.

## Dependencies

- `Z6 EFTX Armored Zs V2`
- EFTX pack dependencies used by the active profile.

## Validation

- Confirm `ZZZZZZZZ_WakaEftxAudioSourceFix` loads in the latest client log.
- Scan the latest log for `AudioSource`, `sound`, `ERR`, and `WRN` after loading a world with EFTX armored zombies available.

## Safety Notes

- This mod should stay late in load order so it can override EFTX sound entries.
- If EFTX updates its prefab paths upstream, re-check whether this patch is still needed.

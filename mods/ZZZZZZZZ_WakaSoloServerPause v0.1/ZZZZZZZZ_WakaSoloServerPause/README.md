# Waka Solo Server Pause

## Purpose

Provide a solo-server AFK safety mode without forcing a relog or trying to fully freeze the dedicated server world.

## Changes

- Adds `/pause` and `/waka_pause` chat commands.
- When exactly one player is connected, protects the player by blocking damage, pausing buff/stat ticking, making the player ignored by AI, and stopping enemy movement.
- Includes client-side Esc menu linkage through `ingameMenu` state and `NetPackageChat` command transport.

## Dependencies

- 7DTD dedicated or solo-server runtime with EAC off.
- Harmony DLL loading.
- Gears/CATUI menu interactions should be re-tested when those mods update.

## Validation

- Confirm `ZZZZZZZZ_WakaSoloServerPause` loads in client and dedicated logs.
- With one player connected, use `/pause` or open the in-game menu and confirm AFK protection engages.
- Confirm protection disengages when leaving the menu or issuing the command again.
- On a dedicated server, scan `output_log_dedi__*.txt` for this mod and for exceptions around chat, GUI, or entity ticking.

## Safety Notes

- This is AFK safety, not a complete world pause.
- Behavior intentionally depends on exactly one connected player.
- Keep manual chat commands available even if Esc menu linkage changes.

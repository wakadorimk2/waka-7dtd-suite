# Known issues

Things that have already cost time. Read before adding mods to an existing world.

## Lock Slot Save States breaks pre-existing saves

**Symptom**: After installing Lock Slot Save States ([Nexus 8893](https://www.nexusmods.com/7daystodie/mods/8893)) on top of a save that was created without it, the game crashes during load with:

```
EndOfStreamException: Attempted to read past the end of the stream.
  at PooledBinaryReader.ReadByte
  at PooledBinaryReader.ReadBoolean
  at EntityCreationData.read
  at VehicleManager.read
  at VehicleManager.Load
  at VehicleManager.Init
```

After the crash, 7DTD writes a corrupted (truncated, ~9-byte) `vehicles.dat` during the failed shutdown, which means **a second launch attempt without recovery will also fail**.

**Cause**: Lock Slot Save States Harmony-patches the binary serialization of `EntityCreationData` to add extra fields. Saves written before the mod was installed don't have those bytes, so the reader runs past end-of-stream. The mod author warns about this on the Nexus page:

> "small chance of player corruption may occur so always backup your saves before adding and removing mods."

**Recovery**:

1. Disable Lock Slot Save States in MO2 (`-` prefix in `modlist.txt`).
2. Restore the truncated save files from their `.bak` siblings in the world folder (`%APPDATA%\7DaysToDie\Saves\<world>\<seed>\`):
   - `vehicles.dat` ← `vehicles.dat.bak`
   - Check `drones.dat`, `turrets.dat` similarly if they're suspiciously small.
3. Decide whether to abandon the save or skip the mod permanently.

**Right way to use this mod**: install **before** creating the world. Once a save has been written without it, the only safe path is a new world.

## Quest "Failed loading objectives" warnings

```
ERR Loading player quests: Quest with ID quest_scourge_infestation_t1: Failed loading objectives
ERR Loading player quests: Quest with ID quest_scourge_infestation_t3: Failed loading objectives
```

These appear when WakaQuestProgression-defined quests are loaded from a save that has them in flight but the runtime XML can't reconstruct objectives. They are **non-fatal** — the game continues. Shows up in our modlist; can be ignored.

## XPath patch warnings (pre-existing)

The current modlist produces a small number of harmless XPath WRN entries, including:

- `BetterFire` items.xml patch on `ammoRocketFrag` (target moved/renamed in vanilla)
- `FastTravel` XUi/windows.xml patches on `CharacterFrameWindow` (UI structure changed)
- `[MODS][Harmony] AccessTools.DeclaredMethod: Could not find method for type ItemActionBetterLauncher` (multiple)

None of these prevent loading. Track here so they aren't re-investigated.

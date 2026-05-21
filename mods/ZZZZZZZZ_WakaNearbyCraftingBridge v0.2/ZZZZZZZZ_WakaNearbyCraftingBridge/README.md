# Waka Nearby Crafting Bridge

## Purpose

Allows Asylum Nearby Crafting to treat these Waka storage blocks as valid nearby crafting containers:

- `wakaIronStorage`
- `wakaSteelStorage`
- `wakaLogisticsContainer`

Asylum Nearby Crafting already detects their `TEFeatureStorage` and `TEFeatureLockable` features, but its `ContainerScanner.IsValidContainerType` method uses a fixed block-name allowlist. This bridge keeps the upstream DLL unchanged and only returns `true` for the three Waka storage block IDs when Asylum's `EnableModdedContainers` setting is enabled.

Locked containers and land-claim access still follow Asylum Nearby Crafting's existing logic.

## Changes

- Allows the three Waka storage blocks through Asylum's container-type filter.
- `ContainerScanner.CountItemInContainers` caches counts by item type and the stable set of nearby containers instead of transient list object identity.
- Cache TTL starts at 0.5 seconds.
- Different item types or different nearby container sets use separate cache entries.
- The cache clears itself if it grows beyond 512 entries.

## Dependencies

- Asylum Nearby Crafting.
- Waka storage block IDs from `WakaStorageRebalanceMVP` or equivalent compatible patch.
- EAC must be off because this is a Harmony DLL patch.

## Validation

- Confirm `ZZZZZZZZ_WakaNearbyCraftingBridge` loads in the latest client log.
- Confirm Asylum Nearby Crafting is enabled and its `EnableModdedContainers` setting is active.
- Place or inspect nearby `wakaIronStorage`, `wakaSteelStorage`, or `wakaLogisticsContainer` and verify crafting scans use their contents.

## Safety Notes

- Current `profiles\Default\modlist.txt` has `ZZZZZZZZ_WakaNearbyCraftingBridge v0.2` disabled.
- This bridge intentionally avoids editing the upstream Asylum DLL.
- Keep cache behavior conservative; stale item counts are more visible to players than a small scan cost.

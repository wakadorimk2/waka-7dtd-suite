# Waka Nearby Crafting Bridge

Allows Asylum Nearby Crafting to treat these Waka storage blocks as valid nearby crafting containers:

- `wakaIronStorage`
- `wakaSteelStorage`
- `wakaLogisticsContainer`

Asylum Nearby Crafting already detects their `TEFeatureStorage` and `TEFeatureLockable` features, but its `ContainerScanner.IsValidContainerType` method uses a fixed block-name allowlist. This bridge keeps the upstream DLL unchanged and only returns `true` for the three Waka storage block IDs when Asylum's `EnableModdedContainers` setting is enabled.

Locked containers and land-claim access still follow Asylum Nearby Crafting's existing logic.

## v0.2 cache behavior

`ContainerScanner.CountItemInContainers` now caches counts by item type and the stable set of nearby containers instead of the transient list object identity. The initial TTL is 0.5 seconds. Different item types or different nearby container sets use separate cache entries, and the cache clears itself if it grows beyond 512 entries.

# Waka Nearby Crafting Bridge

Allows Asylum Nearby Crafting to treat these Waka storage blocks as valid nearby crafting containers:

- `wakaIronStorage`
- `wakaSteelStorage`
- `wakaLogisticsContainer`

Asylum Nearby Crafting already detects their `TEFeatureStorage` and `TEFeatureLockable` features, but its `ContainerScanner.IsValidContainerType` method uses a fixed block-name allowlist. This bridge keeps the upstream DLL unchanged and only returns `true` for the three Waka storage block IDs when Asylum's `EnableModdedContainers` setting is enabled.

Locked containers and land-claim access still follow Asylum Nearby Crafting's existing logic.

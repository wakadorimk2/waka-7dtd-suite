# Waka Storage Rebalance MVP

## Purpose

Adds three dedicated high-density base storage blocks:

- Waka Iron Storage Crate: 96 slots (12x8)
- Waka Steel Storage Crate: 120 slots (12x10)
- Waka Logistics Storage Crate: 144 slots (12x12)

The vanilla writable crate upgrade path is extended as:

- Wood Storage Crate -> Iron Storage Crate -> Steel Storage Crate -> Waka Iron Storage Crate -> Waka Steel Storage Crate -> Waka Logistics Storage Crate

The Waka crate block IDs stay stable (`wakaIronStorage`, `wakaSteelStorage`, and `wakaLogisticsContainer`) so existing placed Waka crates are not renamed or replaced.

Vehicle storage is also rebalanced around one installed vehicle storage mod:

- Bicycle: 12x1 base, 12x2 with the storage mod
- Minibike: 12x3 base, 12x4 with the storage mod
- Motorcycle: 12x5 base, 12x6 with the storage mod
- Gyrocopter: 12x6 base, 12x7 with the storage mod
- 4x4 Truck: 12x11 base, 12x12 with the storage mod

The vehicle storage mod is limited to one per vehicle so the 4x4 does not exceed 144 slots.

The vanilla writable wood, iron, and steel crates keep their original loot lists and capacities.

## Changes

This mod patches `windowLooting` and `windowVehicleStorage` to display larger 12-column container grids up to 12x12. It is built for the current CATUI/SCore stack and does not target SMX compatibility.

## Dependencies

- Current CATUI/SCore UI stack.
- `WakaNearbyCraftingBridge` is optional and only needed when Asylum Nearby Crafting should scan these Waka crates.

## Validation

- Confirm `ZZZZZZZZ_WakaStorageRebalanceMVP` loads in the latest client or dedicated log.
- Check the three Waka crate recipes and block names in game.
- Inspect storage UI for clean 12-column wrapping on 96, 120, and 144 slot containers.
- For dedicated server validation, scan the newest `output_log_dedi__*.txt` for this mod name and XUi/XPath errors.

## Safety Notes

Before shrinking, uninstalling, or removing this mod, empty and remove any placed Waka storage containers. Reducing capacity while items remain in the upper slots can strand or lose stored items, and removing the mod while those blocks still exist can cause missing-block errors.

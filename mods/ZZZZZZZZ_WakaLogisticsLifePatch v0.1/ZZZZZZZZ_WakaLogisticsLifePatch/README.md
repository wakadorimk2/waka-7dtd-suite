# Waka Logistics Life Patch

## Purpose

Keep logistics upgrades useful while restoring packing, return trips, and storage planning as part of daily play.

## Changes

- Patches logistics-related recipes, loot, trader entries, and localization.
- Keeps the base convenience layer from erasing transport and storage decisions.
- Current working tree has uncommitted edits in `Config\recipes.xml` and `Config\Localization.txt`; read those before making further balance changes.

## Dependencies

- Active profile logistics and storage stack.
- Related storage behavior from `WakaStorageRebalanceMVP`.
- Trader/loot changes should be considered alongside the current economy patch.

## Validation

- Confirm `ZZZZZZZZ_WakaLogisticsLifePatch` loads in the latest client log.
- Scan for `XPath`, `XML`, `traders`, `loot`, and the mod name after deploy.
- Check affected recipes and trader availability in game before changing economy values again.

## Safety Notes

- Avoid editing this in the same pass as unrelated economy tuning unless the target overlap is explicit.
- Preserve existing uncommitted user changes.

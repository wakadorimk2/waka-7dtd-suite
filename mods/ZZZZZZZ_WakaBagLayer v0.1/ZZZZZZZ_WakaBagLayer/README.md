# Waka Bag Layer

## Purpose

Provide a 91-slot backpack layer with meaningful over-capacity pressure and no extra backpack mod dependency.

## Changes

- Sets `BagSize` to 91.
- Uses a 7x13 inventory UI layout.
- Lowers starting `CarryCapacity` from 27 to 12.
- Tunes `buffEncumberedInv` so about 20 over-capacity slots becomes nearly immobilizing.
- Rescales Black Wolf Pack Mule and pocket-mod bonuses for the larger bag.

## Dependencies

- `Black Wolf's better vanilla perks` in the current profile.
- Current CATUI inventory layout.
- Optional gear content is split into `ZZZZZZZ_WakaBagLayerGear v0.1`, which is disabled in `profiles\Default\modlist.txt`.

## Validation

- Confirm `ZZZZZZZ_WakaBagLayer` loads in the latest client log.
- Check `BagSize`, `CarryCapacity`, and inventory grid behavior in game.
- Confirm `ZZZZZZZ_WakaBagLayerGear` remains disabled when the intent is only the lightweight core.

## Safety Notes

- Inventory capacity changes are save-facing. Empty or reduce carried items before shrinking the bag.
- Re-check Black Wolf perk values before changing carry capacity again.

# Waka Link Crafting

## Purpose

Adds `wakaLinkedLogisticsContainer` as an explicit top-tier upgrade after `wakaLogisticsContainer`.

The SCore `AdvancedRecipes.ReadFromContainers` feature is enabled, but this mod constrains it to workbench crafting and the nearest linked logistics crate only. Regular vanilla crates, Waka Iron Storage, Waka Steel Storage, and Waka Logistics Storage are not treated as remote crafting sources.

## Behavior

- Crafting source scope: `workbench` only.
- Container source scope: `wakaLinkedLogisticsContainer` only.
- Multiple linked crates in range: the nearest crate wins; exact ties use fixed coordinate ordering.
- Repair/upgrade remote reads stay disabled.
- Asylum Nearby Crafting and `WakaNearbyCraftingBridge` are not required.

When a linked crate is selected, the DLL logs the selected block position. This gives a stable source-of-truth for which crate the recipe UI, ingredient check, and material removal are using.

## Validation

- Confirm `ZZZZZZZZ_WakaLinkCrafting` loads in the latest client or dedicated log.
- Scan the log for `ERR|WRN|XPath|XML|ModInfo|Exception`.
- Place one linked crate near a workbench and confirm workbench crafting can read it.
- Place regular Waka crates nearby and confirm they are not counted.
- Place two linked crates nearby and confirm the log reports only the nearest selected crate.

# Waka Link Crafting

## Purpose

Adds `wakaLinkedLogisticsContainer` as an explicit top-tier upgrade after `wakaLogisticsContainer`.

The SCore `AdvancedRecipes.ReadFromContainers` feature is enabled, but this mod constrains it to configured workstation crafting and all linked logistics crates inside the configured remote-crafting range. Forge crafting is intentionally excluded because it consumes smelted materials instead of direct inventory ingredients. Regular vanilla crates, Waka Iron Storage, Waka Steel Storage, and Waka Logistics Storage are not treated as remote crafting sources.

## Behavior

- Crafting source scope: `workbench`, `campfire`, `chemistryStation`, `cementMixer`, `ammopress`, and `researchbench`. `forge` is excluded.
- Container source scope: `wakaLinkedLogisticsContainer` only.
- Multiple linked crates in range: all linked crates are used, ordered nearest first; exact ties use fixed coordinate ordering.
- Repair/upgrade remote reads stay disabled.
- Asylum Nearby Crafting and `WakaNearbyCraftingBridge` are not required.

When linked crates are selected, the DLL logs the crate count and block positions in consumption order. This gives a stable source-of-truth for which crates the recipe UI, ingredient check, and material removal are using.

## Validation

- Confirm `ZZZZZZZZ_WakaLinkCrafting` loads in the latest client or dedicated log.
- Scan the log for `ERR|WRN|XPath|XML|ModInfo|Exception`.
- Place one linked crate near a workbench and confirm workbench crafting can read it.
- Confirm campfire, chemistry station, cement mixer, ammo press, and research bench crafting can read linked crates.
- Confirm forge crafting does not read linked crates.
- Place regular Waka crates nearby and confirm they are not counted.
- Place two linked crates nearby, split ingredients between them, and confirm workbench crafting can use both.
- Confirm material removal uses the nearest linked crate first.
- Confirm linked crates outside the configured range are not counted.

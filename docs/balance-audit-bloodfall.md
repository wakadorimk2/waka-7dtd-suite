# Bloodfall + Weapon Mods Balance Audit

Date: 2026-05-27

Scope: static XML audit of the current `profiles/Default/modlist.txt` enabled stack. No XML, modlist, or game files were edited.

## 1. Executive Summary

The current stack points toward a compressed late-game answer: high-penetration / high-single-shot shotgun and rifle ammunition, especially AP-style slugs and EFTX AP calibers, answers too many different pressures at once.

This is not only a raw damage issue. The XML suggests several independent systems all reward the same answer:

- Bloodfall escalates enemies through high-tier radiated regeneration, boss-tier regeneration, and dense spawn pools.
- EFTX Armored Zs adds armor-specific resistance and armor-damage mechanics that reward penetration and armor-damage ammo.
- IZY/EFTX ammo includes extreme single-shot slug and explosive outliers, such as EFT AP20/Barricade slugs and high explosive ordnance.
- WakaPerkBoost removes Bloodfall's `perkZVArmorPenetration` and pushes penetration into stronger `perkPenetrator` scaling.
- Black Wolf perks and explosives add broad damage, bleed, dismember, and high explosive damage on top of that.
- Ammo Press, Ammo Press EFT/IZY compatibility, Ammo Recycling, WakaAmmoFlow, and WakaAmmoRealPrice make specialty ammo more legible and somewhat constrained, but still craftable and lootable.

The likely balance risk is that normal ammo, HP ammo, melee, bleed-only, turret-only, and non-AP sustained-fire builds become situational or economy-filler choices once armor, regeneration, high HP, and boss pressure are all present together.

## 2. Load Order Context

The active profile loads high-prefix Waka patches before many base content mods in file order, but MO2 deployment order is still the current profile fact for this audit. Relevant active entries include:

| Area | Active mods observed |
| --- | --- |
| Bloodfall / enemy scaling | `Bloodfall - Hardcore Overhaul`, `Enhanced Zombie Scaling (2.5 Compatible)`, `Z6 EFTX Armored Zs V2`, `ZZZZZZZ_WakaBloodfallTuning v0.1`, `ZZZZZZZ_WakaRadiatedRegenTuning v0.1`, `ZZZZZ_WakaTierCurve v0.1` |
| Weapons / ammo | `EFTX Pack Standard V2`, `EFTX_Pack_Core V2`, `Z2 EFTX IZY Uses All The Ammo`, `IZY-All in One Gun Pack v5.1`, `z_7D2.5_Izayo_WeaponFixes`, `ZZZ_WakaGunFlowPatch v1.1` |
| Ammo economy | `(V2) Oakraven Ammo Press`, `Ammo Press Add-On Patch for EFT and Izy v2`, `LittleRedSonja Ammunition Recycling`, `ZZZZZZZ_WakaAmmoFlowPatch v0.1`, `ZZZZZZ_WakaAmmoRealPrice v0.1` |
| Perks / explosives | `Black Wolf's better vanilla perks`, `Black Wolf's better explosives`, `Black Wolf's faster resources and ammo scrapping and crafting`, `ZZZZZZZ_WakaPerkBoost v0.1` |
| Economy pacing | `ZZZZZZZZ_WakaEconomyPacingPatch v0.1`, `ZZZZZZ_WakaSlowCurve v0.1`, `ZZZZZ_WakaQuestDifficultyTuning v0.1` |

Important ordering note: this audit reads file facts and patch intent, but it does not build a full merged XML. Where multiple mods patch the same node, conclusions are stated as likely unless a direct later patch is visible.

## 3. Observed Dominant Strategies

| Scenario | Current compressed answer | Evidence | Likely dead / reduced choices |
| --- | --- | --- | --- |
| Armored target | AP ammo, AP slug, penetrator scaling | `Z6 EFTX Armored Zs V2/Config/items.xml` adds armor damage and `TargetArmor` effects; `ZZZZZZZ_WakaPerkBoost/Config/progression.xml:143-145` increases `perkPenetrator` armor reduction and penetration count. | Normal ammo, HP ammo, bleed-only melee if armor blocks time-to-kill. |
| High HP flesh target | High single-shot slug or high caliber rifle | `Z2 EFTX IZY Uses All The Ammo/Config/items.xml:6761-6763` sets `ammoEftAP20Slug` to very high damage; `6814-6816` does the same for `ammoEftBarricadeSlug`; `6838-6869` covers high-damage rifle ammo. | Low-caliber sustained fire unless ammo supply is abundant. |
| Regenerating target | Burst damage plus armor bypass | Bloodfall entity tiers set `RadiatedRegenAmount` from tier 6 upward; Waka later clamps many tiers but still leaves Bloodlord at 22 and `bossDevastator` at 30 in `ZZZZZZZ_WakaRadiatedRegenTuning/Config/entityclasses.xml:13-24`. | Damage-over-time and slow attrition, unless DOT also suppresses regen at runtime. |
| Boss / Bloodlord | AP burst or explosive burst | Bloodlord classes have 20,000 XP and regen values 50-100 in Bloodfall `entityclasses.xml:6568-7695`; Waka clamps final Bloodlord regen to 22 in `WakaRadiatedRegenTuning`. Black Wolf explosives define 500-3000 entity damage in `Better explosives + landmines/Config/items.xml:23-69`. | Mid-tier normal ammo, support-only utility, pure crowd-control. |
| Horde / swarm | Explosives, turret fire, high-capacity sustained fire | Black Wolf explosives add large entity-radius explosives; Bloodfall entitygroups contain dense tiered additions; WakaBloodfallTuning rewrites global and biome groups in `entitygroups.xml:22-114`. | Precision-only rifles when swarm density exceeds reload economy. |
| Fast pressure | Shotgun burst / knockdown / high RPM specials | IZY shotgun items show high RPM shotgun baselines in several packs; Black Wolf perk additions attach bleed and dismember effects around `progression.xml:3636-3739`. | Slow melee or single-shot weapons without disable/knockdown. |
| Rifle / AR / SMG sustained fire | AP-capable calibers | WakaAmmoFlow still leaves AP variants in loot at scaled low/medium/large counts, for example `ammo762mmBulletAP`, `ammo556mmArmorPiercing`, and `ammo45ACPArmorpiecing` in `loot.xml:77-181`. | Ball / normal variants become economy backup rather than tactical choice. |
| Turret / trap | AP turret ammo and Bloodfall turret scaling | Bloodfall `items.xml:375-381` gives robotic turret damage, magazine, and RPM; EFTX Armored Zs patches `ammoJunkTurretAP` target armor around `items.xml:1695-1696`. | Non-AP turret ammo against armored targets. |
| Melee | Bleed and stealth synergies, but squeezed by regen/armor | Black Wolf Deep Cuts and general bleed hooks add `bleedCounter` and `buffInjuryBleeding`; Bloodfall reduces Deep Cuts direct entity damage in `progression.xml:1706-1708`. | Melee without armor shred, AP-like perk support, or strong sustain. |

## 4. Ammo Role Matrix

| Ammo role | Observed role | Compression risk |
| --- | --- | --- |
| Normal ball ammo | Cheap baseline, lootable and craftable. | Falls behind once armor and regen appear together. |
| HP / high-power ammo | Flesh-damage role exists in vanilla/EFT/IZY naming. | If AP damage is close enough and armor is common, HP becomes a narrow optimization. |
| Buckshot | Crowd / close-range role. IZY premium shells set `EntityDamage` 13 and Dragon's Breath 12 in `7D1.0_Izayo_WeaponpackRemastered_SHOTGUNpackVAL/Config/items.xml:39-107`. | Can be displaced by AP slug if slug handles armor, HP, and bosses. |
| Vanilla slug | Precision shotgun role. Ammo Press makes it craftable at `V2-OakravenAmmoPress/Config/recipes.xml:138-144`. | Competes poorly with AP/Barricade/EFT slug outliers. |
| Breaching / AP slug | Armor and block role. Ammo Press makes `ammoShotgunBreachingSlug` craftable at `recipes.xml:150-157`; Waka prices it at 35 in `WakaAmmoRealPrice/Config/items.xml:36`. | If it also remains efficient against flesh, it becomes a general-purpose shotgun answer. |
| EFT AP20 / Barricade slug | Extreme single-shot answer. `ammoEftAP20Slug` is set to 256 entity damage and `ammoEftBarricadeSlug` to 273 in `Z2_EFTX_IZY_Uses_All_The_Ammo/Config/items.xml:6761-6816`. | Strong candidate for collapsing armor, boss, and high-HP roles into one ammo family. |
| Explosive / grenade / RPG | Horde and boss burst. EFT/IZY compatibility adds craftable 25mm, RPG, 40mm, and special explosive ammo through `craft_area="ammopress"`; Black Wolf explosives adds high entity damage. | Can erase swarm and boss distinction if supply is reliable. |
| Fire ammo | Burning / DOT role. Bloodfall items show `BuffProcChance` for burning at 0.75 near `Bloodfall/Config/items.xml:73-115`; IZY Dragon's Breath exists. | If DOT does not counter regen directly, fire may be visual/status-only versus high regen. |
| Bleed special | Deep Cuts and Black Wolf perk hooks add bleed counters and buffs. | Bleed is partly squeezed by armor, regen, and burst thresholds; shotgun-linked bleed makes shotgun even more general. |
| Turret AP | Anti-armor support. EFTX Armored Zs patches junk turret AP to `TargetArmor -.5`. | Turret normal/shell ammo loses a role against armored enemies. |

## 5. Enemy Durability Taxonomy

| Enemy class | What the XML emphasizes | Current answer | Intended counter, inferred |
| --- | --- | --- | --- |
| Armored zombies | EFTX armor level CVars and armor-damage effects. | AP ammo, penetrator, AP turret ammo. | Armor-breaking or armor-piercing ammo. |
| Flesh high HP | Bloodfall upper tiers extend vanilla classes with much higher XP rewards. | High-caliber rifle, AP slug, explosives. | Sustained DPS or specialized high-tier weapons. |
| Regenerating radiated | Bloodfall sets regen by tier; Waka clamps but preserves regen taxonomy. | Burst damage above regen threshold. | Focus fire, regen suppression if any exists at runtime. |
| Swarm | Bloodfall and Waka entitygroups add tiered enemies into broad groups. | Explosive / shotgun / turret. | AoE, positioning, traps. |
| Fast pressure | Bloodfall includes fast/high-tier variants and special bosses. | Shotgun burst, disables, high RPM. | Crowd control and movement discipline. |
| Support / special | Screamer, contaminator, cryptlord, lighteater, cerberon, minions. | Priority burst. | Target priority, special counterplay. |
| Boss / Bloodlord | Bloodlord and boss classes show very high XP and high regen; `bossDevastator` is especially high. | AP burst or explosive burst. | Dedicated boss-kill loadout, not general ammo. |

Static XML suggests enemy durability is not a single ladder. It is a stack of armor, regen, HP, speed, and density. The issue is that AP burst answers too many of those axes simultaneously.

## 6. Skill Synergy Audit

| Synergy | Evidence | Balance hypothesis |
| --- | --- | --- |
| Shotgun + AP slug | WakaPerkBoost raises `perkBoomstick` entity damage to `.3,1.5`; EFT/IZY slug variants include very high AP-style values; Ammo Press makes slug families craftable. | Shotgun can become both close-range swarm tool and precision boss/armor tool. |
| Explosives + bleed | Black Wolf explosives add high entity damage; Black Wolf perks add bleed counters from non-DeepCuts hits and shotgun-related dismember paths. | AoE burst plus incidental bleed may reduce the need for specialist DOT builds. |
| AP + headshot / critical | EFTX armor patches add targeted armor behavior; `perkPenetrator` is boosted and entity penetration count rises to `2,6`. | AP precision fire may outperform HP or normal ammo even when target armor is not the only problem. |
| Bleed + AoE | Black Wolf perk hooks add bleed on dismember and attacks across multiple weapon families. | Bleed becomes passive bonus on already-good tools rather than a primary build identity. |
| Regen reduction + burst | WakaRadiatedRegenTuning lowers Bloodfall's raw regen values but keeps high-tier regen meaningful. | Burst remains mandatory when regen and armor coexist. |
| Ammo crafting/salvage + AP economy | Ammo Press and compatibility recipes make vanilla, EFT, IZY, and explosive ammo craftable; Ammo Recycling returns large component bundles; Black Wolf scrap/craft lowers resource ingredient time to 0.01 for key scraps. | Specialty ammo scarcity may be weaker than intended during long play, especially after loot-to-scrap-to-press loops are established. |

## 7. Economy Audit

Ammo economy has two opposing forces:

- WakaAmmoFlow reduces direct loot counts and probability for specialty ammo, e.g. shotgun slug/breaching slug counts drop to `1,2` small, `2,5` medium, `5,10` large in `WakaAmmoFlowPatch/Config/loot.xml:187-220`.
- WakaAmmoRealPrice raises ammo prices by caliber and specialty, e.g. 7.62 AP 14, shotgun shell 18, slug 30, breaching slug 35, premium shell 35, Dragon's Breath 40, explosive fragmentation 50 in `WakaAmmoRealPrice/Config/items.xml:31-106`.
- Ammo Press still makes AP, HP, slug, breaching slug, junk turret AP, EFT AP, EFT HP, EFT shotgun specials, grenades, and rockets craftable via `craft_area="ammopress"`.
- Ammo Recycling returns large material bundles from ammo families, including bullet tips, casings, gunpowder, buckshot, forged steel, rocket parts, and special resources.
- Black Wolf faster scrap/craft sets core scrap ingredient times like brass, lead, iron, and polymers to `0.01`, lowering the friction of material conversion.
- `ZZZZZ_WakaSlayerEZSPatch` adds challenge rewards that include `ammoEftAP20Slug` and `ammoJunkTurretAP` quantities, so AP supply is not only loot/craft/trader based.

Likely result: early and mid-game specialty ammo may be constrained by Waka loot/pricing, but late-game infrastructure can still normalize AP and explosive use. The strongest loop is:

`loot/trader ammo -> recycle into components -> faster scrap/craft resource handling -> Ammo Press -> AP / slug / explosive ammo -> solves armor + HP + regen + boss pressure`

Challenge rewards add a second branch:

`slayer/challenge reward -> AP20 slug / AP turret ammo -> bypasses normal loot scarcity -> feeds the same armor/HP/regen answer`

This loop does not prove AP is over-supplied in runtime. It does show that multiple economy mods support converting diverse ammo/resource finds into the same dominant answers.

## 8. Missing Counterplay / Dead Choices

| Choice / counterplay | Why it may be weak |
| --- | --- |
| Normal ball ammo | Cheaper but does not answer armor or regen efficiently. |
| HP ammo | Its intended flesh niche overlaps with high-damage AP and slug ammo. |
| Vanilla slug | Outclassed by EFT AP20/Barricade-style slugs if those are available. |
| Bleed-primary melee | Bloodfall reduces Deep Cuts direct damage while armor/regen punish long time-to-kill. |
| Fire-primary ammo | Burning exists, but static XML did not prove a direct anti-regen role. |
| Turret normal ammo | Turret AP gets explicit armor support. |
| Precision rifle without AP | High HP/armor/regen together favor AP or explosive burst. |
| Resource scarcity as counterweight | Ammo Press/Recycling/Black Wolf scrap speed reduce the long-term cost of specialty ammo conversion. |

## 9. Balance Hypotheses

These are cause hypotheses only, not patch recommendations.

1. AP-style ammo is acting as the general-purpose late-game ammo because armor, high HP, regeneration, and boss pressure appear together.
2. Shotgun identity is overloaded: it handles swarm pressure with buckshot/shells, armor/high HP with AP/Barricade slugs, and bleed/dismember through perk hooks.
3. Explosives may be a second universal answer because Black Wolf's high entity damage stacks against Bloodfall swarm density and bosses.
4. WakaAmmoFlow and WakaAmmoRealPrice correctly push against direct loot/trader abundance, but crafting/recycling may re-open abundance later.
5. Bloodfall's enemy taxonomy likely expects different answers for armor, regen, bosses, and swarms; the weapon/ammo stack may collapse those answers into AP burst and explosive burst.
6. Perk consolidation around stronger `perkPenetrator` may make Bloodfall's original armor-penetration identity less distinct.
7. Non-AP sustained-fire builds need a separate reason to exist beyond cheaper ammo, because cheaper ammo does not solve regen thresholds or armor time-to-kill.

## Files Inspected

- `profiles/Default/modlist.txt`
- `mods/Bloodfall - Hardcore Overhaul/Bloodfall/Config/items.xml`
- `mods/Bloodfall - Hardcore Overhaul/Bloodfall/Config/buffs.xml`
- `mods/Bloodfall - Hardcore Overhaul/Bloodfall/Config/progression.xml`
- `mods/Bloodfall - Hardcore Overhaul/Bloodfall/Config/entityclasses.xml`
- `mods/Bloodfall - Hardcore Overhaul/Bloodfall/Config/entitygroups.xml`
- `mods/Enhanced Zombie Scaling (2.5 Compatible)`
- `mods/Z6 EFTX Armored Zs V2/Z6_EFTX_Armored_Zs/Config/items.xml`
- `mods/Z2 EFTX IZY Uses All The Ammo/Z2_EFTX_IZY_Uses_All_The_Ammo/Config/items.xml`
- `mods/IZY-All in One Gun Pack v5.1`
- `mods/EFTX Pack Standard V2`
- `mods/EFTX_Pack_Core V2`
- `mods/(V2) Oakraven Ammo Press/V2-OakravenAmmoPress/Config/recipes.xml`
- `mods/Ammo Press Add-On Patch for EFT and Izy v2/zzz_Ammo_Press_Add-On_Compatibility_Patch/Config/recipes.xml`
- `mods/LittleRedSonja Ammunition Recycling/LittleRedSonja_AmmoRecycling/Config/items.xml`
- `mods/Black Wolf's better explosives (1.0 and 2.0)/Better explosives + landmines/Config/items.xml`
- `mods/Black Wolf's better vanilla perks (A21 - 1.0 - 2.0)/Better perks/Config/progression.xml`
- `mods/Black Wolf's faster resources and ammo scrapping and crafting (A21 - 1.0 - 2.0)/Better scrap&craft/Config/items.xml`
- `mods/ZZZZZZZ_WakaAmmoFlowPatch v0.1/ZZZZZZZ_WakaAmmoFlowPatch/Config/loot.xml`
- `mods/ZZZZZZ_WakaAmmoRealPrice v0.1/ZZZZZZ_WakaAmmoRealPrice/Config/items.xml`
- `mods/ZZZZZZZ_WakaBloodfallTuning v0.1/ZZZZZZZ_WakaBloodfallTuning/Config/entitygroups.xml`
- `mods/ZZZZZZZ_WakaRadiatedRegenTuning v0.1/ZZZZZZZ_WakaRadiatedRegenTuning/Config/entityclasses.xml`
- `mods/ZZZZZZZ_WakaPerkBoost v0.1/ZZZZZZZ_WakaPerkBoost/Config/progression.xml`
- `mods/ZZZZZ_WakaSlayerEZSPatch v0.1/ZZZZZ_WakaSlayerEZSPatch/Config/quests.xml`
- `mods/ZZZZZZZZ_WakaEconomyPacingPatch v0.1`
- `mods/ZZZ_WakaGunFlowPatch v1.1`

## Important XML Paths

- `/items/item[@name='ammoEftAP20Slug']/effect_group[@name='ammo']/passive_effect[@name='EntityDamage']`
- `/items/item[@name='ammoEftBarricadeSlug']/effect_group[@name='ammo']/passive_effect[@name='EntityDamage']`
- `/items/item[@name='ammoShotgunBreachingSlug']`
- `/items/item[@name='ammo762mmBulletAP']`
- `/items/item[@name='ammo556mmArmorPiercing']`
- `/items/item[@name='ammo45ACPArmorpiecing']`
- `/items/item[@name='ammoJunkTurretAP']/effect_group/passive_effect[@name='TargetArmor']`
- `/progression/perks/perk[@name='perkBoomstick']/effect_group/passive_effect[@name='EntityDamage']`
- `/progression/perks/perk[@name='perkDemolitionsExpert']`
- `/progression/perks/perk[@name='perkPenetrator']/effect_group/passive_effect[@name='TargetArmor']`
- `/progression/perks/perk[@name='perkDeepCuts']`
- `/entity_classes/entity_class[contains(@name,'Bloodlord')]/effect_group[@name='Base Effects']/triggered_effect[@cvar='RadiatedRegenAmount']`
- `/entity_classes/entity_class[@name='bossDevastator']/effect_group[@name='Base Effects']/triggered_effect[@cvar='RadiatedRegenAmount']`
- `/entitygroups/entitygroup[@name='ZombiesAll']`
- `/entitygroups/entitygroup[contains(@name,'feralHordeStageGS')]`
- `/lootcontainers/lootgroup/item[@name='ammo762mmBulletAP' or @name='ammoShotgunBreachingSlug']`
- `/recipes/recipe[@craft_area='ammopress']`
- `/items/item/property[@class='Action0']/property[@name='Create_item']`

## Unknowns / Limitations

- No full merged XML was generated; late patch order is inferred from active mod files and known MO2 profile state.
- No game launch, XML load log, or runtime DPS test was performed.
- Real hit rate, pellet count behavior, armor calculation details, dismember behavior, and projectile explosion falloff were not measured.
- Current player perks, gear quality, mods, books, buffs, difficulty, and gamestage were not inspected.
- DLL effects from Waka mods, SCore, RAM, or other runtime systems were not decompiled for this audit.
- Loot table probability after all mods merge was not numerically simulated.
- Trader inventory after all traderstage patches was not simulated.
- Ammo recycling return rates were inspected statically but not tested in-game.

## No Files Modified Except This Report

Only `docs/balance-audit-bloodfall.md` was created for this audit. No mod XML, profile, deployment, save, or game files were intentionally changed.

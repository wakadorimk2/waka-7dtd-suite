# レシピ補填・経済バランス v0.2

日付: 2026-05-28

範囲: `Waka Lean EFTX World v2` の #8 監査。Default と NotebookServer の有効 modlist を前提に、既存 scanner と Waka patch の最小更新で確認した。

## 実施内容

- `tools/waka-economy-scan.ps1` を更新し、Quest Revamp の raw `casinoCoin` 行ではなく、Waka quest patch 適用後の effective reward を `reward` finding として表示するようにした。
- `_analysis/waka-economy-risk-report.html` を Default profile で再生成した。
- `_analysis/waka-economy-risk-report-notebook.html` を NotebookServer profile で生成し、サーバー側 profile でも同じ経済上位候補を確認した。
- `ZZZZZZZZ_WakaEconomyPacingPatch` に craftable EFTX shotgun resale cap を追加した。

## Scan Snapshot

| Profile | Report | XML files | Priced items | Applied item/block patches | Quest reward patches |
| --- | --- | ---: | ---: | ---: | ---: |
| Default | `_analysis/waka-economy-risk-report.html` | 627 | 1446 | 977 | 69 |
| NotebookServer | `_analysis/waka-economy-risk-report-notebook.html` | 622 | 1446 | 977 | 69 |

## Patch Applied

| Item | Before effective value | After effective value | Reason |
| --- | ---: | ---: | --- |
| `gunAA12gen1` | 12000 | 10000 | Craftable EFTX shotgun resale outlier; align with existing T4/T5 cap band. |
| `gunstriker12` | 12000 | 10000 | Same recipe and resale issue as `gunAA12gen1`. |

The corresponding recipe findings dropped from output value `12000` / known margin `11350` to output value `10000` / known margin `9350`. The scanner still treats several passive recipe requirements as unknown inputs, so these are audit signals rather than exact profit proofs.

## Findings

| Area | Current finding | Classification | Next action |
| --- | --- | --- | --- |
| Quest Revamp high-difficulty `casinoCoin` | Waka patch is applying. Highest effective high-difficulty payouts remain `6000` for ultra/nightmare tier 6-10 and `4800` for superinfested tier 6-10. | Maintain for now | Do not lower again until long-play quest income feels dominant. Scanner now shows effective values, not raw 10k-11k source rows. |
| Craftable EFTX/IZY T4+ equipment | Many T4 recipes remain visible around the 10k value band. | Maintain / watch | This is expected after the established 10k cap. Revisit only if crafting-to-vendor loops become actual play behavior. |
| `gunAA12gen1` / `gunstriker12` | Remaining 12k craftable shotgun outliers. | Limited | Implemented 10k cap in WakaEconomyPacingPatch. |
| Ammo Press + compatibility recipes | Ammo Press and EFT/IZY add-on provide broad special ammo crafting. | Maintain with constraints | Keep because #4 ammo role tuning already reduced AP/explosive dominance. If ammo scarcity still collapses, adjust special ammo recipes or craft areas rather than deleting Ammo Press. |
| Ammunition Recycling | Mod advertises roughly 80% returns and active XML contains many material bundle outputs, including some compatibility rows that output `casinoCoin`. | Needs targeted audit | Do not remove yet. Next pass should inspect actual active bundle outputs for EFTX/IZY special ammo and patch only casinoCoin/material over-returns if exploitable. |
| POI Scourge StationCosts | Skill point bundle costs 15 tokens; vehicles cost 30/70/200/300/450. WakaQuestProgression grants Scourge Beacon tokens as `tier * 2`. | Maintain / watch | Current beacon token flow means skill points are not immediate spam, and higher vehicles require many high-tier clears. Keep while validating POI Scourge as progression/terrain content. |
| Scourge skill point bundle | `scourgeSkillPointBundle` is already non-sellable through WakaEconomyPacingPatch. | Maintain | No additional economy patch needed now. |
| Ore Processing removal | Ore Processing is absent from active modlist. Black Wolf still makes core resource scrap/craft very fast. | Separate #9 follow-up | Ore Processing is closed as excluded. Resource sink concern shifts to Black Wolf fast resource handling plus ammo loops, not Ore Processing itself. |

## Decision

#8 is partially closed for the simple resale layer: the scanner now reports effective quest rewards, and the last obvious craftable EFTX shotgun resale pair is capped. No broad recipe deletion is recommended in this pass.

The next useful #8/#9 continuation is a narrow Ammunition Recycling audit: identify active bundle outputs that create `casinoCoin` or high-value special-ammo materials, then decide whether to set those bundles non-sellable, remove coin outputs, or increase special-ammo input costs.

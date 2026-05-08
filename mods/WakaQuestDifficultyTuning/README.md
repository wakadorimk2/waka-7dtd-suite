# Waka Quest Difficulty Tuning v0.4.0

[Quest Revamp - Gears Edition](https://www.nexusmods.com/7daystodie/mods/7210) ＋ [FNS Make Quest Rewards Great Again](https://www.nexusmods.com/7daystodie/mods/6779) のコンパニオン。FNS の「全 tier 報酬選択肢 = 10」フラット化を tier 別スケーリングに置き換える。

## 解決する問題

FNS は報酬選択肢を 10 に揃えるが、結果として「tier1 と tier6 で報酬の豪華さに差が無い」感覚が出る。難易度が上がるほど旨みが増えるべきという基本設計から外れる。

## 機能

### Per-tier scaling

| tier | regular | super | ultra/nightmare |
|---|---|---|---|
| 1 | 3 | +2 | +3 |
| 2 | 4 | +2 | +3 |
| 3 | 5 | +2 | +3 |
| 4 | 6 | +2 | +3 |
| 5 | 7 | +2 | +3 |
| 6 | 8 | +2 | +3 |

→ tier6 nightmare は 11 選択肢、tier1 regular は 3 選択肢。

### tier1 の退屈緩和

`tier1_clear_superinfested` を追加。tier1 でも super infestated は意味のあるチャレンジに。

### ultrainfested の調整

- spawn / GS を「choosable hard quest」帯に再調整（取るか見送るか判断が成立するレンジ）
- Trader rotation で super 系の重みを倍化

### 各セルに value-scaled lane

各 tier × difficulty セルに以下の lane：

- weapon
- armor
- tool
- resource

加えて：

- tier4+ の super/ultra/nightmare に `groupCraftingRare`
- tier6 の super/ultra/nightmare に `resourceLegendaryParts` 固定

## 必要環境

- 7 Days to Die v2.6
- Quest Revamp - Gears Edition v2.6.1+
- FNS Make Quest Rewards Great Again v2.5+
- EAC OK（pure XML）

## インストール

`mods/WakaQuestDifficultyTuning/` を MO2 経由でインストール。Quest Revamp と FNS より**後**にロードすること。

## 関連

- [WakaQuestProgression](../WakaQuestProgression/README.md) と組み合わせ可（あちらは Beacon→Quest 派生 + counter +α、こちらは報酬選択肢の per-tier 調整）

## ライセンス

MIT — repo root の `LICENSE` 参照。

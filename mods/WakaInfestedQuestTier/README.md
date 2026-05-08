# Waka Infested Quest Tier v0.1.0

POI Scourge の Infestation Beacon が Trader Tier 進行に寄与するようにする Harmony パッチ。

> ⚠️ このmodの機能は **Waka Quest Progression** に統合済みです。WakaInfestedQuestTier は履歴目的で残されており、WakaQuestProgression を使ってください。

## 動作概要

`ItemActionStartScourgeQuest` を tier-aware な変種に置き換える：

- 現在の POI の `difficulty_tier` を読む
- `quest_scourge_infestation_t1` 〜 `t6` のどれにルーティングするか決定
- 各派生 quest に `add_to_tier_complete=true` と対応する `difficulty_tier` を埋め込む

これにより、Beacon クリア時に POI の難易度に応じて Trader Tier ポイントが正しく加算される。

## 必要環境

- 7 Days to Die v2.6
- HarmonyX（`0_TFP_Harmony`）
- POI Scourge v1.4+
- EAC OFF（DLL mod）

## ビルド

```pwsh
cd mods/WakaInfestedQuestTier/Source
dotnet build -c Release
```

## ライセンス

MIT — repo root の `LICENSE` 参照。

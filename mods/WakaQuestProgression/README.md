# Waka Quest Progression v0.3.0

Quest progression tuning bundle。POI Scourge ＋ Quest Revamp の進行系を 2 軸で補正する Harmony パッチ。

## 機能

### (1) POI Scourge Beacon の tier-aware quest 派生

`ItemActionStartScourgeQuest` を reflection 経由で再定義し、Beacon 起動時に：

- POI の `difficulty_tier` を読み取る
- `quest_scourge_infestation_t1` 〜 `t6` の対応 quest にルーティング
- 各派生 quest に `add_to_tier_complete=true` と対応する `difficulty_tier` 埋込
- 完了時に scourge tokens を grant（tier に応じた量）

これにより Beacon クリアが Trader Tier 進行に正しく反映される（バニラ trader quest 経由とは別経路、double-reward を回避）。

### (2) Quest Revamp super/ultra/nightmare の counter +α

Quest Revamp の難易度別カウンタに、完了時の追加加算：

- `super` 完了 → +1
- `ultra` 完了 → +2
- `nightmare` 完了 → +2

`difficulty_tier` 自体は変えない（trader-tier gating を維持）。難クエを取った人がより速く tier 進行する設計。

## 必要環境

- 7 Days to Die v2.6
- HarmonyX（`0_TFP_Harmony`、IZY-AIO Gun Pack v5.1 同梱）
- POI Scourge v1.4+
- Quest Revamp - Gears Edition v2.6.1+（オプション、(2) 機能のみ依存）
- EAC OFF（DLL mod）

## ビルド

```pwsh
cd mods/WakaQuestProgression/Source
dotnet build -c Release
```

詳細は [docs/build-guide.md](../../docs/build-guide.md)。

## デプロイ

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "<your MO2 mods folder>" -Only WakaQuestProgression
```

## 関連

- 旧 `WakaInfestedQuestTier` を吸収。同時に有効にしないこと
- `WakaQuestDifficultyTuning` と組み合わせ可（あちらは XML 側、こちらは C# 側）

## ライセンス

MIT — repo root の `LICENSE` 参照。

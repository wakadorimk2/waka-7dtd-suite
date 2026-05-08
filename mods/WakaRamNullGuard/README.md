# Waka RAM Null Guard v0.3.0

[RAM (Random Affixes Mod)](https://www.nexusmods.com/7daystodie/mods/9567) の null check 漏れを Harmony Prefix で塞ぐ防御パッチ。

## 解決する問題

RAM v0.5.x には複数の null check 漏れがあり、戦闘系イベントで NRE が散発する。

| 対象 | 症状 |
|---|---|
| `AffixUtils.ChallengeGroupIsCompleted` | `challengeJournal` null で例外 |
| `AffixSystem.ApplyAffixToLootCheck` | プレイヤー以外の loot spawn で `player=null` ログスパム |
| `AffixResurgence.ResurgenceCheck` | `_damageSource` null on damage event |
| `AffixBringItDown.BringItDownCheck` | 同上 |
| `AffixPermadeath.PermadeathCheck` | 同上 |
| `AffixGiantSlayer.GiantSlayerCheck` | 同上 |
| `AffixBulletRecovery.BulletRecoveryCheck` | `_actionData` チェーン null on every shot |

## 機能

7 箇所の RAM 内部メソッドに対して Harmony Prefix を被せ、null 引数を検知したら：

- early return（false で原メソッド skip）または安全な default 動作に置換
- ログスパムを抑制
- ゲーム本体は通常通り進行

すべての Prefix は **reflection でターゲットを解決**するため、このmodは RAM への compile-time 依存を持たない。RAM 不在時は自動的に無効化（safe-fail）。

## 必要環境

- 7 Days to Die v2.6
- HarmonyX（`0_TFP_Harmony`）
- RAM - Random Affixes Mod v0.5.x（無くても起動可、その場合 patch は inert）
- EAC OFF（DLL mod）

## ビルド

```pwsh
cd mods/WakaRamNullGuard/Source
dotnet build -c Release
```

## デプロイ

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "<your MO2 mods folder>" -Only WakaRamNullGuard
```

## ライセンス

MIT — repo root の `LICENSE` 参照。

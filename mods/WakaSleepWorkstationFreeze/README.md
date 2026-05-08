# Waka Sleep Workstation Freeze v0.1

[Sleep Overhaul](https://www.nexusmods.com/7daystodie/mods/8461) の time skip 中に、`TileEntityWorkstation` / `TileEntityForge` の燃料消費・レシピ進行を抑止する Harmony パッチ。

## 解決する問題

Sleep Overhaul の time skip は `BedrollSleepTimeSkip.Advance(World world, float seconds)` 内部で各 workstation/forge を巡回し、`HandleFuel(world, seconds)` などを `seconds` 秒分まとめて呼ぶ。結果、`sleep` するたびに campfire / forge / chemistry station の燃料が大量に焼ける。

## 仕組み

`Advance` メソッドの **Prefix で `return false`** を返し、time skip 中の workstation 進行を完全停止する。

- world clock 自体は Sleep Overhaul の別経路（`World.SetTime`）で進む → ゲーム内時間は正常
- workstation 燃料は温存
- 副作用：sleep 中の crafting も進まない（`HandleRecipeQueue` 呼ばれない）

燃料温存と引き換えのトレードオフ。

## 必要環境

- 7 Days to Die v2.6
- HarmonyX（`0_TFP_Harmony`）
- Sleep Overhaul v1.3+（無くても起動可、`Sleep Overhaul assembly not found, patch will be inert.` 警告のみ）
- EAC OFF（DLL mod）

## ビルド

```pwsh
cd mods/WakaSleepWorkstationFreeze
dotnet build WakaSleepWorkstationFreeze.csproj -c Release
```

7DTD が既定パス（`C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\`）以外にある場合、csproj の `<SDTDManagedDir>` を編集するか、環境変数 `SDTDManagedDir` で上書き。詳細は [docs/build-guide.md](../../docs/build-guide.md)。

## デプロイ

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "<your MO2 mods folder>" -Only WakaSleepWorkstationFreeze
```

## 動作確認

1. 7DTD 起動
2. Player.log（`%APPDATA%\..\LocalLow\The Fun Pimps\7 Days To Die\Player.log`）で確認：
   - `[WakaSleepWorkstationFreeze] Harmony patches applied.`
   - Sleep Overhaul 未導入時は inert 警告
3. campfire 点火、燃料残量を控える
4. Sleep Overhaul で長時間 sleep
5. 起床後、燃料残量がほぼ減っていなければ OK

## 既知の制約

- Sleep Overhaul 側のクラス名 `BedrollSleepTimeSkip` が将来変更されると `Prepare()` が false を返して patch 無効化（safe-fail 設計）
- マルチプレイ時、time skip は host 側でしか走らないので host にこのmodが入っていれば良い

## ライセンス

MIT — repo root の `LICENSE` 参照。

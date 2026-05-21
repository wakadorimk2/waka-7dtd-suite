# Waka Sleep Workstation Freeze v0.1

Sleep Overhaul の time skip 中に、`TileEntityWorkstation` / `TileEntityForge` の燃料消費・レシピ進行を抑止するHarmonyパッチ。

## 仕組み

ilspycmd で Sleep Overhaul DLL (`Bedroll Sleeping.dll`) を解析した結果、`BedrollSleepTimeSkip.Advance(World world, float seconds)` が time skip 中に各 workstation/forge を巡回し、`HandleFuel(world, seconds)` などを `seconds` 秒分まとめて呼んで燃料を消費させていることを確認。

このmodは `Advance` メソッドの **Prefix で `return false`** を返し、time skip 中の workstation 進行を完全停止する。world clock 自体は Sleep Overhaul の別経路 (`World.SetTime`) で進むので、ゲーム内時間は問題なく進む。

副作用：
- sleep 中、workstation の crafting も進まない（HandleRecipeQueue 呼ばれない）
- 燃料温存と引き換えのトレードオフ

## ビルド済みDLL

`WakaSleepWorkstationFreeze.dll` がこのフォルダに出力済み。MO2で有効化すれば即動作する。

## 再ビルド方法

7DTDが既定パス（`C:\Program Files (x86)\Steam\steamapps\common\7 Days To Die\`）にインストールされている前提：

```bash
cd "C:/Modding/MO2/mods/ZZZZZ_WakaSleepWorkstationFreeze v0.1/ZZZZZ_WakaSleepWorkstationFreeze"
dotnet build WakaSleepWorkstationFreeze.csproj -c Release
```

別パスの場合、csproj の `<SDTDManagedDir>` を編集するか、環境変数 `SDTDManagedDir` で上書き。

## 動作確認

1. MO2でこのmodを有効化（`ZZZZZ_WakaSleepWorkstationFreeze v0.1` にチェック）
2. 7DTD起動
3. Player.log で以下のいずれかを確認：
   - `[WakaSleepWorkstationFreeze] Harmony patches applied.`
   - Sleep Overhaul未導入時は `Sleep Overhaul assembly not found, patch will be inert.` の警告（無害）
4. campfire点火、燃料残量を控える
5. Sleep Overhaul で長時間sleep
6. 起床後、燃料残量がほぼ減っていなければOK

Player.log の場所：`%APPDATA%\..\LocalLow\The Fun Pimps\7 Days To Die\Player.log`

## 既知の制約

- EAC オフ必須（DLL mod のため、ModInfo.xmlで宣言済み）
- Sleep Overhaul 側のクラス名 `BedrollSleepTimeSkip` が将来変更されると `Prepare()` が false を返してpatchが無効化される（safe-fail設計、ゲームは普通に起動する）
- マルチプレイ時、time skip は host 側でしか走らないので host にこのmodが入っていれば良い

## ファイル一覧

- `ModInfo.xml` — 7DTDがmodとして認識するための宣言
- `Init.cs` — Harmony初期化エントリ (IModApi.InitMod)
- `WorkstationFreezePatch.cs` — Advance メソッドへのHarmonyパッチ本体
- `WakaSleepWorkstationFreeze.csproj` — SDK-style csproj、`Microsoft.NETFramework.ReferenceAssemblies.net48` でtargeting pack補完
- `WakaSleepWorkstationFreeze.dll` — ビルド済み出力
- `WakaSleepWorkstationFreeze.pdb` — デバッグシンボル

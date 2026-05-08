# Waka Beacon v0.1.0

クラフト可能なビーコンブロック。設置すると地図とコンパスに自動登録され、コンパスに上下方向矢印 + HUD に高低差ラベル（`Beacon ▲5m / ▼12m`）を実時間表示する。

## 解決する問題

バニラのマップマーカーは X/Z 座標しか持たず **Y 軸（高度）情報を欠いている**。深い鉱山、多階層 POI、塔状の拠点では、マーカーが地表に貼り付いていても実際は地下何メートルにいるのか分からない問題があった。

このmodは Beacon ブロックを設置するだけで「Beacon が今どれくらい上 or 下にあるか」を常時表示する。

## 機能

- `wakaBeacon` ブロックを craft → 任意の場所に設置
- 設置と同時に NavObject 登録（マップ・コンパス両方に表示）
- コンパス上で Beacon の方角に矢印 ▲/▼ で上下表示
- HUD オーバーレイで `Beacon ▲5m` のようなラベル
- 撤去で NavObject も削除

## 必要環境

- 7 Days to Die v2.6
- HarmonyX（`0_TFP_Harmony`、IZY-AIO Gun Pack v5.1 同梱）
- EAC OFF（DLL mod）

## ビルド

```pwsh
cd mods/WakaBeacon/Source
dotnet build -c Release
```

詳細は [docs/build-guide.md](../../docs/build-guide.md)。

## デプロイ

```pwsh
pwsh -File scripts/deploy.ps1 -Mo2ModsRoot "<your MO2 mods folder>" -Only WakaBeacon
```

## ライセンス

MIT — repo root の `LICENSE` 参照。

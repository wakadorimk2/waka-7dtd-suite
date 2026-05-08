# Waka Gun Flow Patch v1.0.0

IZY と EFTX の銃を T1-T5 loot / trader flow に統一するパッチ。

## 解決する問題

IZY-AIO Gun Pack v5.1 と EFTX Pack の銃が、それぞれ独自の loot table / trader stock を持っていて：

- いくつかの IZY 銃が orphan 状態（FAR Rifle、Dual USP45、AA12、Super Eagle 44SMG、44 Leveraction Carbine）— vanilla loot に組み込まれていない
- EFTX gun bodies が `groupWeaponsT[N]_Ranged` に流れていない

結果、IZY/EFTX 銃が抽選プールから外れて出現が偏る。

## 機能

- Orphan IZY 銃を `groupWeaponsT[N]_Ranged` に正規ルートで挿入
- EFTX gun bodies を tier 別 ranged group へ振り分け
- Trader stock のラインに組み込み

すべて pure XML、loot table 拡張のみ。

## 必要環境

- 7 Days to Die v2.6
- IZY-All in One Gun Pack v5.1+
- EFTX Pack Core v2.6+（v2.6.2.12+）
- EAC OK（pure XML）

## インストール

`mods/WakaGunFlowPatch/` を MO2 経由でインストール。IZY と EFTX より後ろにロードすること。

## ライセンス

MIT — repo root の `LICENSE` 参照。

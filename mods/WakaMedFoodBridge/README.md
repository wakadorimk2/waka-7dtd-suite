# Waka Med and Food Bridge v1.0.0

EFTX Meds / Food と vanilla 医療・食料経済を双方向ブリッジする mod。

## 解決する問題

EFTX Meds Plus / EFTX Food は独自の素材経済を持ち、vanilla の `medicalBandage` / `foodBread` 等とは隔離されている。結果として：

- EFTX 素材が余っても vanilla アイテムを作れない
- vanilla 素材が余っても EFTX アイテムを作れない

このmodは双方向のオルタナルートを提供。

## 機能

- EFTX 素材で vanilla の医療・食料アイテムを craft 可
- vanilla 素材で EFTX 医療・食料アイテムを craft 可
- gating は vanilla の `craftingMedical` / `craftingFood` に従う

すべて pure XML の追加レシピ。元のレシピは保持される。

## 必要環境

- 7 Days to Die v2.6
- Z1 EFTX Meds Plus V2 v2.5+
- Z3 EFTX Food V2 v2.5+
- EAC OK（pure XML）

## インストール

`mods/WakaMedFoodBridge/` を MO2 経由でインストール。

## ライセンス

MIT — repo root の `LICENSE` 参照。

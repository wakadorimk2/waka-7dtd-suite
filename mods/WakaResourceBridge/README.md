# Waka Resource Bridge v1.0.0

EFTX 武器系リソース（WD40 / Multitool / Toolset / Repair Kits）と vanilla 経済を双方向ブリッジ。

## 解決する問題

EFTX は独自の武器修理素材（WD40 等）に依存するが、vanilla 経済では入手・代替がしづらい。逆も同様で、`resourceResearchData` のような vanilla リソースが EFTX 装備で必要になっても EFTX 内では作れない。

## 機能

- EFTX のボトルネック素材に **vanilla-ingredient ルート**を追加（vanilla 素材で EFTX 素材を craft 可）
- `resourceResearchData` の craft / loot 経路を補強（EFTX 装備での消費に対応）
- gating は vanilla の `craftingRepairTools` / `craftingWorkstations` に従う

すべて追加レシピ、元のレシピは保持。

## 必要環境

- 7 Days to Die v2.6
- EFTX Pack Core v2.6+
- EAC OK（pure XML）

## インストール

`mods/WakaResourceBridge/` を MO2 経由でインストール。

## ライセンス

MIT — repo root の `LICENSE` 参照。

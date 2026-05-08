# Waka Food Stat Display Fix v0.2.0

食料 tooltip に static `display_value` タグを注入する保険パッチ。UI mod が `triggered_effect` ベースの tooltip 解決を撹乱しても、食料スタットが見えるようになる。

## 解決する問題

7DTD のバニラ食料は `triggered_effect` で食事効果を表現するが、CATUI 等の UI mod がツールチップ表示をフックすると、効果が表示されない／壊れて表示される現象がある。

このmodは vanilla 食材、EFTX 食材、Medical 改修食材すべてに静的 `display_value` を埋め込む。tooltip 解決経路に依らず数値が見える保険。

## 必要環境

- 7 Days to Die v2.6
- EAC OK（pure XML append）

## インストール

`mods/WakaFoodStatFix/` を MO2 経由でインストール。

## ライセンス

MIT — repo root の `LICENSE` 参照。

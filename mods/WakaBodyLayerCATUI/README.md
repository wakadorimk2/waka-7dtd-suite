# Waka Body Layer - CATUI Bridge v0.1.0

[Waka Body Layer](../WakaBodyLayer/README.md) と [CATUI](https://www.nexusmods.com/7daystodie/mods/5248) を繋ぐオプションブリッジ mod。CATUI HUD 左下の status row に Protein / Carb / Fat 栄養スタットを追加する。

## 機能

- CATUI HUD bottom-left status row に 3軸栄養値を表示
- 0-80 = 緑、80-100 = 赤の色帯バー
- `CATUI_playerCurrentLife` dummy 経由で常時更新
- Body Layer Core 経由で buff 付与

deficit ゾーン（赤/オレンジ）と excess ゾーン（黄/紫）を一目で確認可能。

## 必要環境

- 7 Days to Die v2.6
- [Waka Body Layer](../WakaBodyLayer/README.md) v0.5.0+
- CATUI v2.5+
- Quartz v7.1+
- EAC OK（pure XML）

## インストール

`mods/WakaBodyLayerCATUI/` を MO2 経由でインストール。Body Layer 本体と CATUI を先に入れること。

## ライセンス

MIT — repo root の `LICENSE` 参照。

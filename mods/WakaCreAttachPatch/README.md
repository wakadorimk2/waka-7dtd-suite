# Waka Cre Attach Patch v0.2.0

[Cre_More Weapon Mods](https://www.nexusmods.com/7daystodie/mods/9373) / [Cre_More Armor Mods](https://www.nexusmods.com/7daystodie/mods/9625) のコンパチビリティパッチ。Durability Overhaul 統合と Black Wolf 風条件ボーナスを追加する。

## 機能

### Durability Overhaul 統合

Cre の `modCre*` / `cre_mod*` attachments を Durability Overhaul の世界観に組み込む：

- `DegradationTags` 付与
- `DegradationMax` 設定
- `ShowQuality` 表示
- 階層別 effect group

→ vanilla mods と同じく**壊れて修理が必要**になる。

### Black Wolf 風条件ボーナス

選択された high-impact attachment に、flat値を増やす代わりに**条件付きボーナス**を上乗せ：

- ヘッドショットダメージ
- ロングレンジダメージ
- dismember chance
- weapon handling
- elemental resist
- movement speed

flat値は触らないので既存バランスを壊さない。

### v0.2 — リッチ説明文

Cre は元々 attachment 名のみで description key が無かった。Black Wolf 風の rich text を全 108 attachment に追加。

## 必要環境

- 7 Days to Die v2.6
- Cre_More Weapon Mods v1.2+
- Cre_More Armor Mods v1.1+
- Durability Overhaul v1.2+
- Black Wolf's better vanilla attachments v3.0+（ボーナス機能の参照基準）
- EAC OK（pure XML）

## インストール

`mods/WakaCreAttachPatch/` を MO2 経由でインストール。Cre_More 系・Durability Overhaul・Black Wolf attachments より**後**にロードすること（inner folder 名 `ZZZZZZZZ_*` で alphabetical 末尾に配置）。

## ライセンス

MIT — repo root の `LICENSE` 参照。

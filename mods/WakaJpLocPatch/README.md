# Waka JP Localization Patch v0.1.0

導入済み全 mod の日本語ローカライゼーションを 1 パッチに集約。

> ⚠️ **WIP**：23 セクション中 17 確認済み・3 クラッシュ確定・3 未確認。Localization 上書きは元 mod の CSV 列構造によっては起動クラッシュを起こすため、追加時は段階的検証必須。

## 機能

各 mod が個別に持つ Localization.txt を読み解き、日本語訳の override を一つの mod にまとめる。`Mods/` フォルダ内の inner folder 名 `ZZZZZZ_WakaJpLocPatch` により alphabetical 末尾でロード → 同 Key+File 行を上書き。

## 必要環境

- 7 Days to Die v2.6
- 翻訳対象の各 mod（任意の subset）
- EAC OK（pure Localization.txt）

## インストール

`mods/WakaJpLocPatch/` を MO2 経由でインストール。

## 既知の制約

- 元 mod の CSV 列構造（カラム数、quote 規則）が違うと `IndexOutOfRangeException` で起動クラッシュ
- 検証手順：`docs/known-issues.md` 参照（bisect_patch.py スタイルの二分探索）

## ライセンス

MIT — repo root の `LICENSE` 参照。

# Waka Slayer EZS Patch v0.1.0

[ExtraSlayerChallenges](https://www.nexusmods.com/7daystodie/mods/8761) を [Enhanced Zombie Scaling](https://www.nexusmods.com/7daystodie/mods/7519) の T2-T5 変異体まで対応させるパッチ。

## 解決する問題

ExtraSlayerChallenges は vanilla のゾンビ種類を前提にチャレンジを定義しているが、Enhanced Zombie Scaling (EZS) で生成される T2-T5 強化変異体（feralBigZombie 系等）はチャレンジカウンタに含まれない。結果、EZS 込みの環境では Slayer チャレンジの進行が極端に遅くなる。

## 機能

EZS の T2-T5 変異体を ExtraSlayerChallenges のチャレンジ judge にマッピングし、討伐カウントに加算されるようにする。

## 必要環境

- 7 Days to Die v2.6
- ExtraSlayerChallenges v1.2+
- Enhanced Zombie Scaling (2.5 Compatible) v1.3+
- EAC OK（pure XML）

## インストール

`mods/WakaSlayerEZSPatch/` を MO2 経由でインストール。両 mod より後にロードすること。

## ライセンス

MIT — repo root の `LICENSE` 参照。

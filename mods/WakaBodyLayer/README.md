# Waka Body Layer v0.5.1

[Medical conditions](https://www.nexusmods.com/7daystodie/mods/7322) の同居コンパニオン。**Protein / Carb / Fat** の3軸栄養レイヤーを既存 Vitamin に重ねて追加し、栄養バランスを継続管理する仕組みを導入する。Fallout 4 MAIM mod の思想を 7DTD に翻訳した設計。

## 解決する問題

バニラ 7DTD の食料は「Food % を回復するか否か」しか軸がなく、種類が増えても**選ぶ理由が無い**。Medical conditions の Vitamin で部分的に解決していたが、たんぱく質・炭水化物・脂質の差が出ない。

このmodは食料それぞれに3栄養素タグを振り、持続的な不均衡で deficit / excess buff を発火させる。

## 機能

### 栄養3軸タグ

すべての vanilla 食材、EFTX 食材、Medical 改修食材に Protein / Carb / Fat の値を付与。Tooltip に表示。

### Deficit / Excess buff（持続的不均衡で発火）

| タイプ | レベル | 効果 |
|---|---|---|
| Protein Deficit | Light / Severe | StaminaMax -5% / -10%、Healing -10% / -25% |
| Carb Deficit | Light / Severe | RunSpeed -5% / -15% |
| Fat Deficit | Light / Severe | ColdResist -5% / -15% |
| Excess（同栄養素過多）| Light / Severe | 各種 penalty + EntityDamage downscale |

### Perk 連動

- `perkSlowMetabolism (Iron Gut) Lv3+` → 基礎 decay 緩和、excess 閾値 softening
- `perkPhysician Lv3+` → injury / infection 時の栄養消費ペナルティ半減

両 perk を取った endgame プレイヤーは持続戦闘を支えられる設計。

### v0.5.1 の変更点

- 栄養素値全体を半減（より細かいバランス）
- Decay を baseline + Food-stat-linked に分離
- 低スタミナ活動で追加 drain
- `Metabolic Cleanse Tonic`（craftable）で過剰時のリセット
- Vitamin 処理は Medical conditions に委譲

### 死亡時リセット（v0.7）

`onSelfDied` で栄養3軸を 50 にリセット、全 deficit / excess buff を明示 Remove。

## 必要環境

- 7 Days to Die v2.6
- Medical conditions v1.3+
- EAC OK（pure XML）

オプション：
- [Waka Body Layer CATUI Bridge](../WakaBodyLayerCATUI/README.md) — HUD 表示
- Bloodfall - Hardcore Overhaul — 相性◎

## インストール

`mods/WakaBodyLayer/` を MO2 経由でインストール。

## デバッグコマンド

F1 コンソールで（v0.5+）：

```
cvar $wakaProtein           現在値確認
cvar $wakaProtein 30        強制設定（10秒以内に Light deficit 発火）
cvar $wakaProtein 10        Severe に切り替わる
```

## ライセンス

MIT — repo root の `LICENSE` 参照。

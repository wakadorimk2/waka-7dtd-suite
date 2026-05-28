# Durability Overhaul除外判断

日付: 2026-05-28

範囲: issue #10。`Durability Overhaul` 除外維持と、`ZZZZZZZ_WakaDurabilityEquipFix v0.1` を base mod 無効時に残す価値があるかの確認。

## 結論

`Durability Overhaul` は除外維持。`ZZZZZZZ_WakaDurabilityEquipFix v0.1` も両 profile で disabled にした。

理由は、Waka fix が直しているのは Durability Overhaul の repair-limit UI bug であり、base mod が無効な現 baseline では修理メタデータ前提の実効価値がないため。`ModInfo.xml` のとおり DurabilityOverhaul.dll への compile-time dependency はないが、Harmony patch を残す必要もない。

## Profile更新

編集前バックアップ:

- `profiles/Default/modlist.txt.bak.20260528-084906`
- `profiles/NotebookServer/modlist.txt.bak.20260528-084906`

| 対象 | Before | After | 判断 |
| --- | --- | --- | --- |
| `Durability Overhaul` | disabled / disabled | disabled / disabled | 除外維持 |
| `ZZZZZZZ_WakaDurabilityEquipFix v0.1` | enabled / enabled | disabled / disabled | base mod復帰時だけ再検討 |

## Static findings

| Evidence | Interpretation |
| --- | --- |
| `WakaDurabilityEquipFix` patches `ItemActionEntryRepair.RefreshEnabled` and checks `XUiC_EquipmentStack` for `repairs` / `MaxRepairs` metadata. | Durability Overhaul の limited repair metadata がないと、通常は早期 return するだけ。 |
| `ModInfo.xml` states no dependency on `DurabilityOverhaul.dll`, but requires HarmonyX and EAC off. | 無害寄りだが、lean baseline では不要な Harmony hook を常時入れる価値が薄い。 |
| `Durability Overhaul` remains disabled in both `Default` and `NotebookServer`. | 終盤の修理維持作業を戻さない方針と一致。 |

## Re-enable trigger

Durability Overhaul を軽量化して戻す場合だけ、同時に `WakaDurabilityEquipFix` を再有効化する。その場合の条件は次のどれか。

- repair count cap を残すが、終盤維持負荷を下げる新設定を入れる。
- repair-limit UI bug が再発し、装備スロットから上限超過修理できる。
- Durability Overhaul互換の別 mod が同じ `repairs` / `MaxRepairs` metadata を使う。

現 baseline では追加 patch 不要。

# Ore Processing除外後の採掘/クラフト経済確認

日付: 2026-05-28

範囲: issue #9。`Waka Lean EFTX World v2` の `Default` / `NotebookServer` profile を前提に、Ore Processing を戻す必要があるか、または Black Wolf / Ammo Press / Ammunition Recycling 側へ最小 Waka patch が必要かを静的監査した。

## 結論

Ore Processing は戻さない。

採掘価値の問題は、Ore Processing を外したことで「鉱石を加工チェーンへ吸われる問題」から「既存の fast resource handling が素材変換の待ち時間を消しすぎる問題」へ移った。現時点では `ZZZZZZZZZZ_WakaAmmoRoleBalance v0.1` が special ammo の input cost、trader supply、loot/reward supply を既に絞っているため、追加 patch は新規セーブ開始前の必須条件にしない。

## Profile状態

| 対象 | Default | NotebookServer | 判断 |
| --- | --- | --- | --- |
| `FER Ore processing factories v2.6` | absent | disabled | 除外維持 |
| `Black Wolf's faster resources and ammo scrapping and crafting` | enabled | enabled | 維持、ただし long-play watch |
| `(V2) Oakraven Ammo Press` | enabled | enabled | 維持 |
| `Ammo Press Add-On Patch for EFT and Izy v2` | enabled | enabled | 維持 |
| `LittleRedSonja Ammunition Recycling` | enabled | enabled | 維持 |
| `ZZZZZZZZZZ_WakaAmmoRoleBalance v0.1` | enabled | enabled | active mitigation |

## Static findings

| 領域 | Evidence | Risk | Decision |
| --- | --- | --- | --- |
| Ore Processing | `Default` には folder entry 自体がなく、`NotebookServer` は `-FER Ore processing factories v2.6`。 | 重い加工チェーンを戻すと、採掘の即時価値がまた加工待ちへ寄る。 | 戻さない。 |
| Black Wolf resource craft speed | `Better scrap&craft/Config/recipes.xml` が `resource*` / `ammo*` recipe の `craft_time > 5` を `2` 秒へ、未指定を `0.5` 秒へ潰す。 | 素材の一括変換が速く、採掘量そのものより queue friction が軽くなる。 | 今は維持。新規セーブで ammo sustain が崩れる場合だけ Waka patch 候補。 |
| Black Wolf scrap ingredient time | `resourceScrapBrass`, `resourceScrapLead`, `resourceCloth`, `resourcePaper`, `resourceScrapIron`, `resourceRockSmall`, `resourceWood`, `resourceScrapPolymers` の `CraftingIngredientTime` が `0.01`。 | loot-to-scrap-to-press loop の摩擦がほぼ消える。 | long-play watch。Ore Processing再導入では解かない。 |
| Ammo Press / EFTX add-on | EFTX/IZY special ammo は `craft_area="ammopress"` で広く craftable。AP20/Barricade slug、EFTX grenades/rockets も対象。 | special ammo scarcity を workstation access と素材コストだけで支える必要がある。 | `WakaAmmoRoleBalance` の input-cost制限を維持。 |
| Ammunition Recycling | #8 監査どおり active EFTX/IZY bundles は素材返却中心で、`casinoCoin` 出力は inactive Vita branch 側。 | 素材回収で long-play sustain が強くなる可能性は残る。 | 追加 patch なし。Vita追加時だけ coin出力を先に潰す。 |

## Patch候補を今入れない理由

1. Ore Processing除外の acceptance は「重い加工チェーンを戻さないこと」が主目的で、Black Wolf の QOL speed を即削除する目的ではない。
2. `WakaAmmoRoleBalance` が AP/slug/explosive ammo の素材入力、trader数、loot/reward供給を先に絞っている。
3. Black Wolf fast resource handling を急に戻すと、鉱石経済だけでなく木材、紙、布、鉄、polymer、通常 ammo crafting の体感まで広く変わる。

## Next trigger

新規セーブで以下が起きた場合だけ、`ZZZZZZZZZZ_WakaResourceFrictionPatch v0.1` のような小 patch を検討する。

- AP / slug / explosive ammo を通常弾のように常用できる。
- loot ammo recycling から Ammo Press への素材循環が採掘・探索判断を置き換える。
- resource/ammo crafting queue がほぼ無意味になり、workstation progression の重みが消える。

初回 patch 候補は Ore Processing再導入ではなく、`resourceScrapBrass` / `resourceScrapLead` / `resourceScrapIron` / `resourceScrapPolymers` と special ammo recipe の時間・素材だけを狭く戻す。

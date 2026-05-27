# Black Wolf perk 10段階化 監査

日付: 2026-05-28

範囲: issue #6 の docs 監査と 10-rank 設計案。第三者 mod は編集しない。

## 結論

`Waka Lean EFTX World v2` では、Black Wolf perk は維持候補。ただし現状の 5-rank 構造は、一部の効果が早い rank に広く乗りすぎる。

今回の推奨は、即時 XML 実装ではなく、次の小パッチ候補 `ZZZZZZZZZZ_WakaBlackWolfPerkRescale v0.1` を切る前提で、以下の順に段階化すること。

1. Trader/economy は既存 `ZZZZZZZZ_WakaEconomyPacingPatch v0.1` を土台にし、5-rank のまま効果を維持するか、後で 10-rank 表示に移す。
2. Penetrator は 100% armor bypass を最終 rank 専用に寄せ、rank 1-6 は armor answer、rank 7-10 は penetration utility として分ける。
3. Boomstick は close-range burst と stumble を残すが、bleed、long-range slug、door/safe utility、penetration を同時に伸ばさない。
4. Demolitions Expert は爆発物の個性を残しつつ、Black Wolf explosives の高い base damage と重なる damage/reload/handling/dismember を薄くする。
5. Strength Mastery 由来の bleed は、perk 専門性を壊す広域 passive なので、Deep Cuts 以外では proc rate と cvar 加算を下げる。
6. 主要対象だけでなく、Black Wolf が性能変更または説明文上書きをしている広めの skill/perk 群も監査し、Localization の記述と実 XML 性能を一致させる。

## 現 baseline

| 対象 | Default | NotebookServer | 判断 |
| --- | --- | --- | --- |
| `Black Wolf's better vanilla perks` | enabled | enabled | #6 の主対象 |
| `Black Wolf's better explosives` | enabled | enabled | Demolitions/Explosive role の外れ値監査対象 |
| `Black Wolf's faster resources and ammo scrapping and crafting` | enabled | enabled | #9 に渡す資源変換リスク |
| `Durability Overhaul` | disabled | disabled | 除外維持 |
| `WalkerSim` | disabled | disabled | 除外維持 |
| `FER Ore processing factories v2.6` | absent in Default | disabled in NotebookServer | Ore Processing は baseline 外 |

## 既存 Waka patch の状態

| Patch | 現在の役割 | #6 での扱い |
| --- | --- | --- |
| `ZZZZZZZZ_WakaEconomyPacingPatch v0.1` | Better Barter を buy `.04,.08,.12,.16,.20`、sell `.02,.04,.06,.08,.10` へ弱体化。Daring Adventurer を TraderStage `8,40`、QuestBonusItemReward `.15,.75` へ弱体化。 | 経済側の初期対策として維持。10-rank 化する場合も、この総量を上限にする。 |
| `ZZZZZZZZZZ_WakaAmmoRoleBalance v0.1` | AP/slug/explosive ammo の loot、trader、recipe、recycling sustain を制限。 | Black Wolf perk の火力側を弱める前提の安全弁。 |
| `ZZZZZZZ_WakaAmmoFlowPatch v0.1` | Black Wolf loot ammo の day 1-3 flood を抑制。 | 序盤 ammo 経済の対策として維持。 |
| `ZZZZZZZZZZ_WakaIzyBlackWolfMeleeBridge v0.1` | IZY melee を Black Wolf melee tag/効果へ橋渡し。 | melee bridge は維持。ただし bleed 強化を増やす patch ではない。 |

## 監査結果

| 領域 | 現状の強さ | リスク | 推奨 |
| --- | --- | --- | --- |
| Better Barter | Black Wolf 本体は buy/sell とも `.05,.10,.15,.20,.25`。Waka patch で buy 上限 20%、sell 上限 10% に低下済み。 | trader 経済、loot 売却、quest 報酬が揃うと金策が早く安定する。 | 現 Waka 値を上限に維持。10-rank 化するなら buy `2,4,6,8,10,12,14,16,18,20`、sell `1,2,3,4,5,6,7,8,9,10`。 |
| Daring Adventurer | Black Wolf 本体は TraderStage `10,50`、QuestBonusItemReward `.2,1`、rank 4/5 で reward choice +1/+2。Waka patch で `8,40`、`.15,.75` へ低下済み。 | trader tier と reward item が同時に進み、探索や crafting sink を圧縮する。 | 現 Waka 値を上限に維持。10-rank 化するなら TraderStage `4..40`、QuestBonus `7.5%..75%`。choice は rank 8/10 へ後ろ倒し。 |
| Penetrator | `perkGunslinger, perkBoomstick, perkDeadEye, perkMachineGunner, turretRanged, perkArchery` へ TargetArmor `-20%..-100%`。shotgun、turret、archery まで広い。 | Insane baseline の通常 armor 対応を越えて、Armored、Large target、Special をまとめて解く。 | armor bypass は rank 10 のみ 100%。rank 1-6 は `-10%..-45%`、rank 7-10 は `-55%,-70%,-85%,-100%`。shotgun pellet と turret は別上限にする。 |
| Shotgun/Boomstick | close range EntityDamage `+20%..+100%`、stun が rank 0 から入り、rank 3+ で強い stun、rank 5 で cripple。item 側で breaching/shot/slug に penetration、bleed、range、door/safe utility が乗る。 | Swarm、Armored、door/safe、bleed、range slug まで shotgun が受け持ちすぎる。 | shotgun は近距離 crowd control に固定。slug の long-range化と Penetrator 由来の door/safe utility は後半か別枠。 |
| Demolitions Expert | Spread `.15..02`、degradation `-10%..-50%`、RPG velocity +10..+30、damage/block damage +50%、handling +50%、reload +35%、dismember +45%。 | Black Wolf explosives の base damage 500-3000、広い radius と重なり、Swarm/Large target の両方を解く。 | 10-rank でも総量を増やさない。damage 上限 +30%、reload +20%、handling +30%、dismember +25% 程度を候補。 |
| Bleed cross-perk | Strength Mastery が `perkSkullCrusher`, `perkPummelPete`, `perkMiner69r`, `corpseRemoval`, `perkBoomstick` に bleed を足す。Boomstick dismember bleed は Strength Mastery rank 1 から有効。 | Deep Cuts の identity が薄れ、shotgunや工具でも bleed が passive bonus になる。 | Deep Cuts 以外の bleed は proc 条件を遅らせる。Strength Mastery rank 1 の dismember bleed は削除または rank 6+ に後ろ倒し。 |
| Fast resource/scrap/craft | resource/ammo craft_time を 2 秒または 0.5 秒へ、brass/lead/iron/wood/polymers 等の ingredient time を `0.01` へ短縮。 | Ammo Press/Recycling と合わせて special ammo sustain を支える。 | #9 に渡す。#6 では perk 火力を抑え、resource loop は別パッチで craft_time 下限を戻す候補。 |

## 10-rank 曲線案

### Trader perks

| Perk | Rank 1-10 案 | 上限 |
| --- | --- | --- |
| Better Barter buy | `2,4,6,8,10,12,14,16,18,20%` | 既存 Waka buy 20% と同じ |
| Better Barter sell | `1,2,3,4,5,6,7,8,9,10%` | 既存 Waka sell 10% と同じ |
| Daring Adventurer TraderStage | `4,8,12,16,20,24,28,32,36,40` | 既存 Waka 40 と同じ |
| Daring Adventurer QuestBonus | `7.5,15,22.5,30,37.5,45,52.5,60,67.5,75%` | 既存 Waka 75% と同じ |
| Quest reward choice | rank 8: +1, rank 10: +2 | 現在の +2 は維持、タイミングだけ後ろ倒し |

### Combat perks

| Perk | Rank 1-10 案 | 役割 |
| --- | --- | --- |
| Penetrator TargetArmor | `-10,-15,-20,-30,-38,-45,-55,-70,-85,-100%` | armor answer。100% は最終投資のみ。 |
| Penetrator penetration count | rank 6: +1 for DeadEye, rank 8: +1 for firearms, rank 10: +2 for DeadEye only | penetration utility。shotgun/turret の万能化を避ける。 |
| Boomstick close damage | `+10,+18,+26,+34,+42,+50,+60,+70,+85,+100%` within 4m | 近距離 burst。最終総量は維持。 |
| Boomstick control | light stun rank 1-3、strong stun rank 6+、cripple rank 10 | Swarm control。序盤 permanent disable を避ける。 |
| Demolitions damage | `+3,+6,+9,+12,+15,+18,+21,+24,+27,+30%` | Swarm/large target の専門性を残すが万能化を抑える。 |
| Demolitions reload | `+2,+4,+6,+8,+10,+12,+14,+16,+18,+20%` | handling quality。Black Wolf explosives base damage と重ねすぎない。 |
| Demolitions dismember | `+2,+4,+6,+8,+10,+13,+16,+19,+22,+25%` | crowd control。damage とは別の価値にする。 |
| Deep Cuts bleed | 現 5-rank bleed 総量を維持し、10-rank に分割 | bleed identity は Deep Cuts に残す。 |
| Strength Mastery cross-bleed | rank 6+ の低 proc、または削除 | off-perk bleed の万能化を抑える。 |

## 広域 skill / localization 監査

Black Wolf は特定 perk だけでなく、多数の attribute mastery、weapon perk、utility perk、item effect、Localization long desc を横断的に変更している。#6 の実装パスでは、数値を変える対象だけでなく、説明文と実性能が一致しているかも成果物に含める。

監査対象は以下の優先順にする。

| 優先 | 対象群 | 理由 | 成果物 |
| --- | --- | --- | --- |
| P0 | Better Barter, Daring Adventurer, Penetrator, Boomstick, Demolitions Expert, Deep Cuts, Strength Mastery bleed | #6 acceptance criteria と直接一致する。 | XML 実性能表、Localization 上書き、patch 候補。 |
| P1 | Dead Eye, Gunslinger, Machine Gunner, Archery, Pummel Pete, Skull Crusher, Miner 69er, Javelin Master | Penetrator、bleed、dismember、range、handling の横断効果を受ける。 | 「perk 別に何が増えたか」と「説明文に書くべき値」の対応表。 |
| P2 | Pack Mule, Grease Monkey, Living off the Land, Physician, Pain Tolerance, Parkour, Lock Picking, Salvage Operations | 経済、探索、移動、防御、QOL に広く影響し、前倒し power になりやすい。 | 強すぎる序盤効果と説明文ズレのリスト。 |
| P3 | Black Wolf item-side hooks in `items.xml`, `blocks.xml`, `recipes.xml`, `buffs.xml`, `entityclasses.xml` | perk 本体の `progression.xml` だけ見ても実性能が読めない。 | perk 名、tag、trigger、rank 条件、実効果の索引。 |

Localization と実性能の一致ルール:

- 数値を変えた passive effect は、対応する `perk*Rank*BWLongDesc` を同じ patch で更新する。
- rank を後ろ倒しした効果は、旧 rank の説明文から削除し、新 rank の説明文へ移す。
- 確率、range、armor bypass、dismember、bleed cvar、reward choice、TraderStage、BarteringBuying/Selling は必ず実値を書く。
- `items.xml` 側で perk に追加される性能も、perk 画面からプレイヤーが期待できる説明へ寄せる。特に shotgun slug range、door/safe damage、ammo penetration、explosive bleed は隠し効果にしない。
- 実装後は `rg` で `BWLongDesc` と変更対象 effect を照合し、説明文に残った古い数値を消す。

実装前に作る確認表:

| Column | 内容 |
| --- | --- |
| `perk` | `perkBetterBarter` などの内部名 |
| `source_file` | `progression.xml`, `items.xml`, `blocks.xml` など |
| `effect` | `TargetArmor`, `EntityDamage`, `BarteringBuying`, triggered bleed など |
| `rank_condition` | `level` または `ProgressionLevel` 条件 |
| `current_value` | Black Wolf + 既存 Waka patch 後の実値 |
| `proposed_value` | #6 patch 候補値 |
| `localization_key` | 更新対象の desc key |
| `localization_status` | `matches`, `stale`, `missing`, `needs rewrite` |

## XML patch 候補

次に実装するなら、第三者 mod 直編集ではなく以下の Waka patch に分離する。

```text
mods/ZZZZZZZZZZ_WakaBlackWolfPerkRescale v0.1/
  ZZZZZZZZZZ_WakaBlackWolfPerkRescale/
    ModInfo.xml
    Config/progression.xml
    Config/items.xml
    Config/Localization.txt
```

初回実装の最小範囲:

1. `progression.xml`: Better Barter / Daring Adventurer の 10-rank 化は見送り、既存 Waka economy 値を維持する。
2. `progression.xml`: Penetrator の TargetArmor を上記 10-rank 相当に再配列する。
3. `progression.xml`: Demolitions Expert の damage/reload/handling/dismember 上限を下げる。
4. `progression.xml` and `items.xml`: Boomstick の Strength Mastery bleed/range/penetration の一部を rank 後半へ後ろ倒しする。
5. `Localization.txt`: Black Wolf long desc を Waka 値に合わせて上書きし、実性能表と照合する。

広域監査を含めた追加範囲:

1. `progression.xml`, `items.xml`, `blocks.xml`, `recipes.xml`, `buffs.xml`, `entityclasses.xml` から `perk*`, `ProgressionLevel`, `BWLongDesc`, `effect_description` を横断抽出する。
2. P0/P1 skill について、rank ごとの実効果と Localization 記述の一致表を作る。
3. 実効果を変更しない skill でも、Black Wolf または既存 Waka patch により説明が stale なら `Localization.txt` だけ修正する。
4. 実性能を変える patch と説明だけ直す patch を同じ mod に入れる場合、コメントで「performance change」と「description-only」を分ける。

## #9 への引き継ぎ

Ore Processing は baseline 外だが、Black Wolf fast resource handling は残っている。特に以下は #9 で確認する。

- resource/ammo recipe の `craft_time` 下限が 0.5-2 秒へ潰れているため、Ammo Press の待ち時間が scarcity として機能しにくい。
- brass/lead/iron/wood/polymers の `CraftingIngredientTime=0.01` は、loot-to-scrap-to-press loop の摩擦をほぼ消す。
- `ZZZZZZZZZZ_WakaAmmoRoleBalance` で input cost と supply は抑えたが、resource conversion speed はまだ別問題として残る。

## #10 / #11 の判断

`Durability Overhaul` と `WalkerSim` は現 baseline では除外維持。#6 の perk 調整では、この2つを戻す前提にしない。必要になった場合のみ、軽量な代替案を別 issue で扱う。

## 検証メモ

- `profiles/Default/modlist.txt`: Black Wolf 主要3系統 enabled、`Durability Overhaul` disabled、`WalkerSim` disabled。
- `profiles/NotebookServer/modlist.txt`: Black Wolf 主要3系統 enabled、`FER Ore processing factories v2.6` disabled、`Durability Overhaul` disabled、`WalkerSim` disabled。
- `docs/recipe-economy-balance-v0.2-20260528.md`: special ammo sustain と Black Wolf resource handling は残リスクとして #6-#9 に引き継ぎ済み。
- XML 実装は未実施のため、`waka-deploy` dry-run と runtime log check は不要。

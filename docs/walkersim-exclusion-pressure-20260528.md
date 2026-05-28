# WalkerSim除外時の敵圧・出現密度代替案

日付: 2026-05-28

範囲: issue #11。`WalkerSim` を戻さず、EFTX / EZS / POI Scourge / 既存 Waka patch の敵圧で足りるかを静的評価した。

## 結論

WalkerSim は除外維持。代替として broad spawn inflation は入れない。

現 baseline は `Enhanced Zombie Scaling`, `Z6 EFTX Armored Zs`, `POI Scourge`, `ExtraSlayerChallenges`, `More Gore`, `Natural Selection`, `WakaTierCurve`, `WakaBiomeChallengeFix`, `WakaQuestProgression` が残っており、常時広域シミュレーションを戻さなくても「装備選択」「POI危険」「quest/scourge導線」の敵圧は維持できる。

## Profile状態

| 対象 | Default | NotebookServer | 判断 |
| --- | --- | --- | --- |
| `WalkerSim` | disabled | disabled | 除外維持 |
| `ZZZZZZZZZZ_Waka_Bloodfall_EFTX_SpawnBridge` | disabled | disabled | そのまま再有効化しない |
| `Enhanced Zombie Scaling (2.5 Compatible)` | enabled | enabled | baseline pressure |
| `Z6 EFTX Armored Zs V2` | enabled | enabled | armored role |
| `POI Scourge` | enabled | enabled | directed encounter / terrain pressure |
| `ExtraSlayerChallenges` | enabled | enabled | opt-in pressure / reward loop |

## Decision table

| Candidate | Keep / reject | Reason |
| --- | --- | --- |
| Re-enable WalkerSim | Reject | 複雑性、常時負荷、出所の読みづらさが lean baseline に合わない。 |
| Broad global spawn increase | Reject | 敵ロールではなく密度だけが上がり、ammo/repair/resource economy を粗く圧迫する。 |
| Re-enable old Bloodfall EFTX bridge | Reject as-is | Bloodfall late-game stack向けの pool/weightで、現 baseline entity定義に対する再検証が必要。 |
| Use POI Scourge / Slayer / biome roles first | Keep | 危険と報酬導線が結びつき、常時負荷を増やさない。 |
| Add light role injection later | Watch candidate | 新規セーブで「夜・wasteland・high-tier POI が薄い」と分かった場合のみ、低率・対象限定で検討。 |

## Future patch shape

不足が実プレイで確認された場合だけ、次の形で小 patch を検討する。

- 対象は `ZombiesNight`, wasteland/downtown biome groups, high-tier sleeper groups, Scourge encounter groups のいずれかに限定する。
- 追加は EFTX/EZS の既存 entityclass が実在する名前だけにする。
- 追加率は低く、常時 world simulation ではなく「場所・時間・報酬導線」と結びつける。
- `Waka_Bloodfall_EFTX_SpawnBridge` は思想参考に留め、既存 XML を再有効化しない。

## #12へ回す確認

新規セーブ開始前チェックでは、runtime log で disabled patch の XPath warning が出ていないこと、POI Scourge / quest 系が正常に読み込まれることを確認する。敵圧の体感判断は新規セーブ後の day 1-3 / first night / first Scourge POI で行う。

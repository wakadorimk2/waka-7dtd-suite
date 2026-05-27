# 次セーブ計画

日付: 2026-05-28

範囲: 次ワールドの計画基準。この文書では XML、modlist、profile、deploy、save、game files は変更しない。

## 結論 (Conclusion)

次セーブは **Waka Lean EFTX World v2** を設計基準にする。EFTX を装備・弾薬・敵ロール調整の中核として維持しつつ、34日目ワールドで耐久維持作業や万能解に寄りすぎた要素は削除または弱体化する。

目標体験は「戦術的な失敗」であり、「ダメージスポンジに負けること」ではない。基準にするのは Burnt Forest の突進バイカー戦である。死亡時には、位置取り、タイミング、優先目標、準備のどれを誤ったかが読めるべきで、AP バーストを増やすだけの耐久チェックにはしない。

`docs/balance-audit-bloodfall.md` は Bloodfall、AP 弾、爆発物、終盤ロール圧縮の詳細根拠として残す。この文書は、次セーブ方針の前向きな単一の判断基準として扱う。

## 背景 (Background)

34日目ワールドは、緊張感のある記憶に残る戦闘を作れることを示した。一方で、いくつかのシステムがプレイを固定化することも分かった。

| 領域 | 学び |
| --- | --- |
| 敵スケーリング | 高耐久、アーマー、再生、密集スポーンが重なると、複数の戦術問題ではなく単一の耐久問題になる。 |
| 弾薬 | アーマー、HP、再生、ボスが重なると、AP 系や高単発火力弾が既定解になりやすい。 |
| 経済 | レシピ補填と弾薬クラフトは有用だが、探索と希少性を消してはいけない。 |
| メンテナンス | 終盤の主作業が修理と耐久維持になると、耐久システムは負担になる。 |
| 複雑性 | パフォーマンス負荷や認知負荷が高い仕組みは、明確なゲームプレイ上の見返りが必要。 |

## 34日目からの学び (Lessons From Day 34)

- Bloodfall は印象的な圧を作ったが、高耐久と再生が AP / 爆発物への固定解を強めた。
- RAM 系は終盤バランスを十分には解決しなかったため、次セーブでは標準削除候補とする。
- Ore Processing は、採掘そのものの楽しさを加工フロー側へ寄せすぎたため削除する。
- WalkerSim は、複雑性とパフォーマンス負荷に対して瞬間ごとのゲームプレイ価値が足りなかった可能性が高い。
- Durability Overhaul は終盤の修理メンテナンスを増やしすぎたため、削除または大幅改修候補とする。
- レシピ補填は進行上の欠落を埋める場合には良い方向性だが、経済バランスと探索動機に結びつける必要がある。
- 最も良かった瞬間は、単に硬い敵ではなく、危険だが読める戦術的圧力から生まれた。

## 次ワールドのテーマ (Next World Theme): `Waka Lean EFTX World v2`

`Waka Lean EFTX World v2` は、EFTX を中心にした薄く読みやすいワールド構成にする。

| 原則 | 方向性 |
| --- | --- |
| 基盤 | EFTX を弾薬、装備、敵ロール調整の中核として維持する。 |
| Lean stack | メンテナンス、不明瞭な難度、パフォーマンス負荷を増やすだけの仕組みは外す。 |
| 戦術的な敵 | 強敵は raw HP ではなく、挙動と状況で判断を迫る。 |
| 弾薬ロールの分離 | 弾薬はそれぞれ異なる問題を解き、単一の最適解へ収束させない。 |
| 経済の摩擦 | クラフトとレシピは、ルート、採掘、トレーダー、探索を残しながら目標を支える。 |

## Mod 採用方針 (Mod Adoption Policy)

次セーブの標準方針:

| Mod / system | 方針 | 理由 |
| --- | --- | --- |
| EFTX | 中核として維持 | 弾薬、装備、ロールベース調整の基盤として最も使いやすい。 |
| Bloodfall | 削除済み | 高耐久と再生が戦闘を固定解へ圧縮した。 |
| RAM-family systems | 削除済み | 複雑性に対して終盤バランス解決力が足りなかった。 |
| Ore Processing | 削除済み | 採掘の直接的な楽しさと価値を下げた。 |
| WalkerSim | 削除済み | 複雑性とパフォーマンス負荷が見返りを上回った。 |
| Durability Overhaul | 削除済み | 終盤の修理維持が作業化した。 |
| Recipe gap-filling patches | 方向性は維持し、範囲を再監査 | 欠落補填は有益だが、経済を平坦化する危険がある。 |
| POI Scourge | 再評価 | 維持するなら報酬、導線、進行上の理由が必要。 |
| Black Wolf perks | 改修候補 | 現状は前半に力が寄りすぎる可能性があるため、薄い10ランク進行を検討する。 |

## 削除 / 弱体化 / 維持候補 (Remove / Weaken / Hold Candidates)

| 候補 | 標準アクション | メモ |
| --- | --- | --- |
| Bloodfall | 削除済み | 敵ロールを耐久壁から分離できる場合のみ一部維持を検討する。 |
| RAM系 | 削除済み | ワールド開始前に明確な穴が見つかった場合のみ戻す。 |
| Ore Processing | 削除済み | 採掘経済は現地価値とクラフト需要を中心に再構成する。 |
| WalkerSim | 削除済み | 必要なら軽い出現密度やロール調整で代替する。 |
| Durability Overhaul | 削除済み | 終盤の遠征計画を修理維持が支配しないようにする。 |
| 高 AP / slug 外れ値 | 弱体化またはロール再定義 | 1つの弾薬系がアーマー、ボス、高 HP、再生をまとめて解かないようにする。 |
| 爆発物外れ値 | 弱体化または供給制限 | 群れ対策の個性は残すが、万能解にはしない。 |
| Black Wolf の前倒し perk power | 再スケール | 序盤の大きな伸びではなく、薄い10ランク化を検討する。 |
| レシピ補填 | 監査付きで維持 | 有用な橋渡しは残し、経済と探索への影響を確認する。 |

## 戦闘バランス方針 (Combat Balance Direction)

戦闘は「何を誤ったか」を問うべきで、「残り HP がいくらか」だけを問うべきではない。

強い遭遇は以下の軸から作る:

- 速度圧
- 奇襲圧
- アーマーと弱点圧
- 群れ圧
- 特殊挙動
- 地形圧
- 優先目標圧

Raw HP と再生は存在してよいが、あくまで軸の1つに留める。敵が高速、重装甲、再生持ち、高 HP を同時に持つなら、明確な counterplay rule が必要である。そうでなければ、再び AP バーストチェックになる。

## 敵ロール設計 (Enemy Role Design)

敵ロールは、異なる道具と判断が意味を持つよう分離する。Bloodfall/RAM/WalkerSim を戻さない現 baseline では、EFTX Armored Zs、Enhanced Zombie Scaling、ExtraSlayerChallenges、POI Scourge、既存 Waka quest/enemy patch を材料にして、まず「どの敵を何として読むか」を固定する。

監査対象の現状態:

| 領域 | 現状態 | #5 での扱い |
| --- | --- | --- |
| `Z6 EFTX Armored Zs V2` | 有効。`eftZombieRandomArmorHead` / `eftZombieRandomArmorBody` により、Insane 相当では `zombieBoe` 系を含む通常ゾンビにも高頻度でランダムアーマーが付く。Soldier/Biker/Demolition や LootGob、BikerBomber、DemolitionGiant、Backpacker、LumberjackAxe は強めまたは特殊寄りの追加枠。 | 主な敵ロール素材。高火力 EFTX への対抗として残すが、Insane では「通常アーマー個体」を日常遭遇の基準として扱い、特殊敵だけの問題にしない。 |
| `Enhanced Zombie Scaling (2.5 Compatible)` | 有効。GS で T2-T5 を出し、HP、PhysicalDamageResist、radiated regen を増やす。Charged/Infernal は tier variant ではなく高 HP 枠。 | 数値スケールの下地。これ単体を敵ロールとは見なさない。 |
| `ExtraSlayerChallenges` + `WakaSlayerEZSPatch` | 有効。EZS T2-T5 と Bloodfall 名を Slayer 対象へ広げる。Bloodfall 名は現 baseline では参考名だけ。 | ロール別の報酬/誘導候補。Bloodfall 再有効化の根拠にはしない。 |
| `POI Scourge` + `WakaQuestProgression` | 有効。POI clear、Infestation Beacon、tier-aware `quest_scourge_infestation_t1..t6`、Scourge Token 導線を持つ。 | Terrain punisher / directed encounter の受け皿。危険追加だけなら維持理由として弱い。 |
| `Waka_Bloodfall_EFTX_SpawnBridge` | 無効。Blood moon、wasteland roaming、high-tier sleepers に EFTX 特殊敵を少量混ぜる設計だった。 | #5 の参考資料のみ。再有効化しない。 |

Insane 基準では、アーマーは特殊敵だけの記号ではない。`zombieBoe*`, `zombieSteve*`, `zombieTomClark*`, `zombieJanitor*`, `zombieInmate*`, `zombieYo*`, `zombieSkate*` などの通常ゾンビにランダムアーマーが付く前提で、通常遭遇の読みに含める。`zombieSoldier*`, `zombieBiker*`, `zombieDemolition*` は通常アーマー個体より強い、または固定寄りの armored-special として別枠で扱う。

### Role Mapping v0.2

| ロール | 現候補 | 圧力 | 対応方針 | 実装ガードレール |
| --- | --- | --- | --- | --- |
| Charger | `zombieBiker`, `zombieBikerFeral`, `zombieBikerBomber`, Burnt Forest の速い個体 | 速度と突進コミット | 回避、地形切り、バースト窓、よろけ。 | 速度役に重装甲、高 HP、再生、密集を同時に足さない。BikerBomber は爆発/接近圧が本体で、硬さを盛らない。 |
| Ambusher | `zombieSpider*`, `zombieSteveCrawler*`, sleeper group、狭い POI 角待ち | 不意打ち、低姿勢、角度 | 音、光、索敵、間合い管理。 | 奇襲役は発見後に短い処理窓を持たせる。高 HP で逃げ場を塞ぐだけの個体にしない。 |
| Armored unit | `zombieBoe*`, `zombieSteve*`, `zombieTomClark*`, `zombieJanitor*`, `zombieInmate*`, `zombieYo*`, `zombieSkate*` など通常ゾンビ + `eftZombieRandomArmorHead` / `eftZombieRandomArmorBody` | 通常遭遇内の正面ダメージ軽減、弾種選択 | AP、アーマー破壊、側面取り、弱点。 | Insane では日常的な処理対象。アーマーそのものは禁止せず、高 HP、再生、速度、密集をさらに重ねて AP チェック化しない。 |
| Swarm | `ZombiesAll`, `ZombiesNight`, burnt/desert biome groups、POI sleeper density | 数、包囲、リロード圧 | 範囲制圧、罠、buckshot、移動。 | 群れの主役は個体耐久ではなく数。EZS 高 tier / radiated regen と密集を重ねる場合は出現量を落とす。 |
| Armored-special | `zombieSoldier*`, `zombieBiker*`, `zombieDemolition*`, `armoredNoFaceshield` tag、固定/強装甲寄り個体 | 強い正面耐性、突進や爆発との複合圧 | AP、アーマー破壊、側面取り、優先処理。 | 通常アーマー個体より強い読み替え役。速度、爆発、高 HP、再生、密集を同時に盛る場合は低頻度かイベント限定にする。 |
| Special | `zombieDemolition`, `zombieDemolitionLootGob`, `zombieMutated*`, `zombieFatCop*`, `zombieScreamer*`, `zombieSoldierLootGob` | ルール破壊、優先処理、報酬/事故 | 優先処理、射線管理、準備済み counter。 | Special は遭遇の読み替え役。通常アーマー個体の延長として常時混ぜる trash にはしない。報酬個体は逃走/耐久/爆発を同時に強くしない。 |
| Terrain punisher | POI Scourge beacon、tier-aware Scourge infestation、wasteland/downtown/night groups、閉所 sleepers | 悪い位置取りへの罰 | ルート選択、扉、高低差、逃走計画。 | 追加報酬または探索導線とセットにする。危険だけ増える POI Scourge は lean 方針に合わない。 |
| Large target | `zombieDemolitionGiant`, `zombieDemolition*`, bear/direwolf 系、将来の boss 枠 | サイズ、遮蔽崩し、優先脅威 | 重弾薬または継続火力へのコミット。 | 汎用 AP 支配にはしない。大型は低頻度、見える前兆、高コスト処理を前提にする。 |

Burnt Forest の突進バイカー死亡は基準点である。高脅威、読める圧力、失敗後に戦術的な学びがあることを目指す。

### Forbidden Stacks

次の組み合わせは、明確な counterplay と出現制限がない限り採用しない。

| 禁止または要審査の重ね方 | 理由 | 例外条件 |
| --- | --- | --- |
| 速度 + 装甲 + 高 HP + 再生 + 密集 | 戦術問題ではなく AP / 爆発物の耐久チェックになる。 | 単体 boss、短時間イベント、逃走可能、報酬が明確、ログで出現率を監査できる場合のみ。 |
| Charger + 爆発 + 高耐久 | 回避失敗より弾薬チェックになる。 | BikerBomber は低～中耐久、低頻度、接近音/見た目で読めること。 |
| 通常アーマー + 高 HP + 再生 + 速度/密集 | Insane の通常アーマー前提を越えて、AP 以外の選択肢が死ぬ。 | 装甲だけなら通常遭遇として許容する。高 tier / radiated regen / 速度 / 密集を重ねる場合は、出現量、頻度、場所、報酬のどれかで強く制限する。 |
| Large target + 常時群れ混入 | 大型の判断が消え、ただの DPS レースになる。 | Scourge、blood moon、wasteland night など高リスク文脈で低頻度に限定する。 |
| POI Scourge + 高 tier sleeper + 報酬薄い | 「なぜやるか」が危険追加だけになる。 | token、tier point、固有報酬、面白い POI 導線のどれかを強化する。 |

### Handoff Criteria

| 引き継ぎ先 | 渡す判断基準 |
| --- | --- |
| #11 WalkerSim 代替 | 常時広域シミュレーションではなく、biome/night/POI/Scourge の既存 group にロールを薄く混ぜる。候補は SoldierLootGob、BikerBomber、DemolitionLootGob、DemolitionGiant だが、`Waka_Bloodfall_EFTX_SpawnBridge` の低率思想だけを参考にする。 |
| #7 POI Scourge | 維持判断は「危険を足す」ではなく、tier-aware infestation、token、trader tier point、面白い POI への導線が敵ロールと結びつくかで決める。Terrain punisher と Special を扱う主戦場にできるなら維持候補。 |
| #6 Black Wolf perk | perk は敵ロールを丸ごと無効化しない。AP/防御/移動/経済のどれか1軸を伸ばすに留め、Charger、Armored、Swarm、Large target すべての万能回答にしない。 |
| 弾薬ロール再設計 | AP は特殊敵専用ではなく、Insane の日常的な通常アーマー個体に対する実用枠にする。buckshot/explosive は Swarm、heavy/rare ammo は Large target を担当する。AP が Swarm / Large target / Special まで全部解く設計にはしない。 |

## 弾薬ロール設計 (Ammo Role Design)

弾薬は明確に異なる仕事を持つよう再設計する。

| ロール | 用途 | ガードレール |
| --- | --- | --- |
| Anti-armor | Insane の通常アーマー個体とアーマー破壊 | 肉体、ボス、群れ、Special までまとめて最適解になってはいけない。 |
| Swarm | 密集集団と近距離圧 | アーマーや大型目標への効率は落とす。 |
| Large target | ボス、大型、重大脅威 | 高コスト、低速、または専門化された選択にする。 |
| General-purpose | 通常探索戦闘 | 有用性は残すが、全場面 best-in-slot にはしない。 |
| Cost-efficient | 長期遠征と低価値戦闘 | ピーク DPS ではなく供給と経済で勝つ。 |

次の設計パスでは、単一の最強弾薬ファミリーを避ける。AP は Insane の通常アーマー個体に対する日常的な実用品として残すが、Swarm、Large target、Special まで全部解く万能弾にはしない。高ダメージ slug、爆発物もそれぞれ、供給、取り回し、対象特化、機会費用のどこかで見えるコストを払うべきである。

## レシピと経済の方針 (Recipe And Economy Direction)

レシピ補填は、以下のどれかを満たす場合は良い方向性である。

- 進行上の欠落を埋める
- modded item を通常経済に参加させる
- 探索入手品に意味のあるクラフト sink を与える
- 希少性を消さずに dead-end loot を減らす

以下のどれかを起こす場合は危険である。

- 多すぎる資源を同じ支配的な戦闘解へ変換する
- 終盤設備で特殊弾薬が日常品になる
- 採掘、ルート、トレード、探索の必要性を消す
- あらゆる不足を workbench 問題に変える

Ore Processing は、採掘をより楽しくする改訂版を作れない限り、次セーブでは削除する。

## POI Scourge 再評価 (POI Scourge Re-evaluation)

POI Scourge は惰性で維持しない。次ワールドでの明確な役割が必要である。

判断質問:

- **通常のクエスト導線との差別化ができているか**
- ルート選択を生むのか、それとも危険を足すだけなのか。
- 報酬はリスクと時間に見合うか。
- 面白い POI へ探索を導けるか。
- 追加の耐久層ではなく、戦術的圧力を作れるか。
- より lean な EFTX 中心の敵スタックと共存できるか。

維持する場合は、次の長期セーブ開始前に報酬または導線の支援が必要である。

## Black Wolf Perk 10段階化 (Black Wolf Perk 10-Rank Rescale)

Black Wolf perk 強化は検討価値がある。ただし、現方針では前半に大きく力が寄る構造から離す。

詳細監査と 10-rank 曲線案は `docs/black-wolf-perk-10-rank-audit-20260528.md` を基準にする。
実装時は主要 perk だけでなく、Black Wolf が横断変更している広めの skill/perk 群も対象にし、Localization の説明と実 XML 性能を一致させる。

目標モデル:

| 設計点 | 方針 |
| --- | --- |
| Rank count | 10 ranks を検討する。 |
| Power curve | 大きな序盤ジャンプではなく、薄く段階的に伸ばす。 |
| Identity | perk の個性は残しつつ、1つの perk が多すぎるシステムを解決しないようにする。 |
| Economy | 最も強い効果は、意味のある投資と進行タイミングに結びつける。 |
| Compatibility | EFTX の弾薬、アーマー、武器ロールとの重なりを再確認する。 |

目的は単純な buff ではなく、長い進行の手触りを作ることである。

## 判断基準 (Decision Criteria)

mod、patch、tuning idea は、以下の多くを満たす場合に `Waka Lean EFTX World v2` へ入れる。

| 基準 | 合格条件 |
| --- | --- |
| Tactical clarity | プレイヤーが死亡または成功の理由を理解できる。 |
| Role separation | 敵、弾薬、perk の明確な役割を作る、または保つ。 |
| Economy health | 希少性、探索、採掘、トレード、クラフトを平坦化せず支える。 |
| Maintenance cost | 終盤プレイを反復的な維持作業にしない。 |
| Performance cost | 実行時負荷に見合う、見えるゲームプレイ上の返りがある。 |
| Stack simplicity | 追加する混乱よりも、減らす混乱の方が大きい。 |
| Long-save stability | 設備と強装備が揃った後も面白さが残る。 |

## 非対象 (Non-Scope)

この文書では以下を行わない。

- XML patch の作成
- `profiles/Default/modlist.txt` の編集
- MO2 profile の変更
- deploy または dry-run deploy
- dedicated server sync
- 既存 mod folder の編集
- balance numbers の確定
- merged XML behavior の証明
- `docs/balance-audit-bloodfall.md` の置き換え

将来の実装はこの方向性から始める。ただし、この文書により実装が行われたとは見なさない。

## 候補 issue (Candidate Issues)

- Bloodfall/RAM除外後のmodlist再構成
- EFTX中心の弾薬ロール再設計
- 敵ロール設計 v0.2
- Black Wolf perk 10段階化
- POI Scourge の報酬/導線強化
- レシピ補填・経済バランス v0.2
- 新規セーブ開始前チェックリスト
- Ore Processing除外後の採掘/クラフト経済確認
- Durability Overhaul除外または終盤負荷軽減案
- WalkerSim除外時の敵圧・出現密度代替案

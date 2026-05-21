# ZZZZZ_WakaPet

7DTD 2.6 向け自作 mod。逃げない・死なない友軍ペット NPC を追加する Companion AI 構想 v0.1 の実装。

LOVOT軸（即物的愛着装置）+ ロッキー軸（相棒装置）を段階的に積む計画。詳細は Claude Code memory `~/.claude/projects/C--Modding-MO2/memory/project_waka_companion_ai.md` 参照。

## 構成

```
ZZZZZ_WakaPet v0.1/
└── ZZZZZ_WakaPet/         <- 7DTD が読む inner mod folder
    ├── ModInfo.xml
    └── Config/
        ├── npc.xml           (新 faction wakaPet)
        ├── entityclasses.xml (entityWakaPetRabbit)
        ├── buffs.xml         (buffWakaPetImmortal)
        └── Localization.txt
```

## v0.1 達成機能

- 新 faction `wakaPet`（全関係 neutral、プレイヤー攻撃対象から構造的に除外）
- `entityWakaPetRabbit`：animalTemplateTimid extend、Class=EntityAnimalRabbit
  - Prefab はバニラ rabbit を流用（v0.5 で静的ロッキー mesh に差し替え予定）
  - AITask を `Look + Wander` のみに差し替え（親の RunawayWhenHurt / RunawayFromEntity を上書き）
  - EntityFlags="animal" で edible を外す（屠殺対象から外す）
- `buffWakaPetImmortal`：vanilla god buff と同じ `GeneralDamageResist 1.0` で完全無敵
  - hidden + remove_on_death=false + duration=0 で恒久付与
- 起動時 `onSelfFirstSpawn` / `onSelfEnteredGame` で自動付与（再ロードでも維持）

## 召喚テスト手順（v0.1 動作確認）

1. ゲーム内で F1 でコンソール開く
2. `dm` で debug menu 有効化
3. `lpi` で playerID を確認
4. `spawnentity <playerID> entityWakaPetRabbit` で召喚
5. プレイヤー近くにうさぎ Prefab が出現
6. **動作確認項目**：
   - 逃げずに Wander する（プレイヤーから離れない）
   - ナイフ・銃で攻撃しても HP が減らず死なない
   - HUD に攻撃者表示などのエラーが出ない
   - ログに XPath エラーなし
7. → 全部 OK なら v0.1 成功

## ログ確認

`C:\Users\wakad\AppData\LocalLow\The Fun Pimps\7 Days To Die\Player.log`

検索クエリ：
- `WakaPet|wakaPet|buffWakaPet|entityWakaPet` — Waka Pet 関連
- `XPath|XML loader|ERR .*[Bb]uff|WRN .*[Bb]uff` — XPath / Buff エラー
- `ERR.*Waka|ERR .*animalTemplate` — entity 起動エラー

## v0.5 への引き継ぎ（ロッキー静的メッシュ差し替え）

`Resources/wakapet_rocky.unity3d` を AssetBundle 化して配置、`entityclasses.xml` の以下2行を差し替えるだけ：

```diff
- <property name="Prefab" value="@:Entities/Animals/Rabbit/animalRabbitPrefab.prefab"/>
+ <property name="Prefab" value="@modfolder:Resources/wakapet_rocky.unity3d?RockyStaticPrefab.Prefab"/>
- <property name="PrefabCombined" value="true"/>
+ <property name="PrefabCombined" value="false"/>
```

これだけで「動く石像ロッキー」状態になる。AI（rabbit ベース）でぴょんぴょん移動するが、メッシュは変形しない静的ロッキー。

Unity ワークフロー（Blender → FBX → Unity → AssetBundle）は別途 `Tools/blender_unity_workflow.md` に書く予定（v0.5 着手時）。

## 依存

- 7DTD 2.6+
- MO2

## ライセンス方針

- **mod 本体（XML / 設定）**：MIT 等で公開可能
- **ロッキー mesh（v0.5 以降）**：Andy Weir / Crown Publishing の権利物（`self_print` 個人利用前提配布）。**Waka Pet 全体として個人利用専用、Nexus 非公開で確定**

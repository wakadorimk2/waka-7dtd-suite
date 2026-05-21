using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// caged Rocky の同一POI 多重 spawn 抑止.
    ///
    /// 1 POI sleeperVolume に複数 sleeper slot があるため、entitygroup の
    /// prob 抽選で entityWakaPetRabbit_caged が複数当たることがある (実機で14体観測).
    /// 半径 30m 以内に他の Waka系 entity (caged or 通常) が居れば、
    /// entityId が小さい方を生存者とし、それ以外を自己 destroy する.
    ///
    /// 動作タイミング: spawn 後 60 フレーム目 (≒1秒後).
    /// firstLateUpdate ではなく遅延させる理由は、同時 spawn された 14体が
    /// 各々の最初のフレームで dedupe を走らせると、お互いが「まだ world に
    /// 登録されてない」状態で誰も検出できず全員 survivor 扱いになるため.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class WakaPet_CagedDedupe
    {
        const float DEDUPE_RADIUS = 30f;
        const float DEDUPE_RADIUS_SQ = DEDUPE_RADIUS * DEDUPE_RADIUS;
        const int DEDUPE_FRAME = 30; // spawn 後 ~0.5 秒で dedupe (sleeper respawn 連鎖を素早く断つ)

        static Dictionary<int, int> frameCounter = new Dictionary<int, int>();
        static HashSet<int> processedIds = new HashSet<int>();

        static void Postfix(EModelBase __instance)
        {
            try
            {
                if (__instance == null) return;
                var go = __instance.gameObject;
                if (go == null) return;
                var entity = go.GetComponent<EntityAlive>();
                if (entity == null) return;

                var ec = EntityClass.list[entity.entityClass];
                if (ec?.entityClassName != "entityWakaPetRabbit_caged") return;

                if (processedIds.Contains(entity.entityId)) return;

                // フレームカウンタを進めて、DEDUPE_FRAME 目だけ実行
                if (!frameCounter.ContainsKey(entity.entityId))
                {
                    frameCounter[entity.entityId] = 0;
                }
                frameCounter[entity.entityId]++;
                if (frameCounter[entity.entityId] < DEDUPE_FRAME) return;

                processedIds.Add(entity.entityId);
                frameCounter.Remove(entity.entityId);

                var world = GameManager.Instance?.World;
                if (world == null) return;

                var myPos = entity.position;
                var bounds = new Bounds(myPos, new Vector3(DEDUPE_RADIUS * 2, DEDUPE_RADIUS * 2, DEDUPE_RADIUS * 2));
                var nearby = new List<Entity>();
                world.GetEntitiesInBounds(typeof(EntityAlive), bounds, nearby);

                int survivorId = entity.entityId;
                int wakaCount = 1; // 自分含む

                foreach (var e in nearby)
                {
                    if (e == null) continue;
                    if (e.entityId == entity.entityId) continue;

                    var ea = e as EntityAlive;
                    if (ea == null) continue;

                    var eClass = EntityClass.list[ea.entityClass];
                    if (eClass?.entityClassName == null) continue;
                    if (!eClass.entityClassName.StartsWith("entityWakaPet")) continue;

                    if ((ea.position - myPos).sqrMagnitude > DEDUPE_RADIUS_SQ) continue;

                    wakaCount++;
                    if (ea.entityId < survivorId) survivorId = ea.entityId;
                }

                if (survivorId != entity.entityId)
                {
                    Log.Out($"[WakaPet/Dedupe] entity {entity.entityId} dedupe (survivor={survivorId}, nearby={wakaCount})");
                    // destroy (Killed/Despawned) だと sleeper system が Restoring で補充するため、
                    // entity を world に残して無効化のみ実施
                    CagedNeutralizer.Neutralize(entity);
                }
                else if (wakaCount > 1)
                {
                    Log.Out($"[WakaPet/Dedupe] entity {entity.entityId} survives ({wakaCount} Waka entities in range)");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Dedupe] Exception: {e}");
            }
        }
    }
}

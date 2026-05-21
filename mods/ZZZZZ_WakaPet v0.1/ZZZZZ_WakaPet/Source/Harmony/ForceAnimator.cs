using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// SCore/vanilla は Mesh プロパティで読んだ Prefab の Animator を strip する.
    /// → Rocky 子 GameObject に Animator を強制 attach + RockyAnimator.controller を流す.
    /// AssetBundle (wakapet_rocky.unity3d) は既に SCore がロード済みなので、ロード済み bundle から Controller を取得.
    /// 加えて毎フレーム IsWalking Bool を entity.motion ベースで sync（Walk/Idle 自動切替）.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class WakaPet_ForceAnimator
    {
        static HashSet<int> initialized = new HashSet<int>();
        static Dictionary<int, Vector3> lastPos = new Dictionary<int, Vector3>();
        static Dictionary<int, float> lastMovingTime = new Dictionary<int, float>();
        static Dictionary<int, float> nextDebugLog = new Dictionary<int, float>();
        static RuntimeAnimatorController cachedController = null;
        static bool bundleSearched = false;
        const float WALK_HOLD_SECONDS = 0.5f;  // 最後に動いてから N 秒は walk 継続

        static void Postfix(EModelBase __instance)
        {
            try
            {
                if (__instance == null) return;
                var go = __instance.gameObject;
                if (go == null) return;
                var entity = go.GetComponent<EntityAlive>();
                if (entity == null) return;
                var entityClass = EntityClass.list[entity.entityClass];
                if (entityClass == null || entityClass.entityClassName == null) return;
                if (!entityClass.entityClassName.StartsWith("entityWakaPet")) return;

                var rockyChild = go.transform.Find("Rocky");
                if (rockyChild == null) return;

                // === 初回 setup ===
                if (!initialized.Contains(entity.entityId))
                {
                    if (!bundleSearched)
                    {
                        bundleSearched = true;
                        foreach (var bundle in UnityEngine.AssetBundle.GetAllLoadedAssetBundles())
                        {
                            var ctrl = bundle.LoadAsset<RuntimeAnimatorController>("RockyAnimator");
                            if (ctrl != null)
                            {
                                cachedController = ctrl;
                                Log.Out($"[WakaPet/Force] Loaded RockyAnimator from bundle '{bundle.name}'");
                                break;
                            }
                        }
                        if (cachedController == null)
                        {
                            Log.Warning("[WakaPet/Force] RockyAnimator not found in any loaded bundle");
                        }
                    }

                    if (cachedController == null)
                    {
                        initialized.Add(entity.entityId);
                        return;
                    }

                    var animSetup = rockyChild.GetComponent<Animator>();
                    if (animSetup == null)
                    {
                        animSetup = rockyChild.gameObject.AddComponent<Animator>();
                        Log.Out($"[WakaPet/Force] Animator added to Rocky child (entity {entity.entityId})");
                    }
                    animSetup.runtimeAnimatorController = cachedController;
                    animSetup.applyRootMotion = false;
                    animSetup.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animSetup.enabled = true;

                    initialized.Add(entity.entityId);
                    Log.Out($"[WakaPet/Force] Setup done: entity={entity.entityId}, controller={cachedController.name}");
                }

                // === 毎フレーム IsWalking sync ===
                var anim = rockyChild.GetComponent<Animator>();
                if (anim == null || anim.runtimeAnimatorController == null) return;

                // 位置差分で判定（entity.position だと NPCCore で更新されないことがあるので mesh world position 使用）
                Vector3 curPos = rockyChild.position;
                Vector3 prevPos = lastPos.ContainsKey(entity.entityId) ? lastPos[entity.entityId] : curPos;
                float distSq = (curPos - prevPos).sqrMagnitude;
                lastPos[entity.entityId] = curPos;

                // entity.motion との比較（デバッグ用）
                float motionSq = entity.motion.sqrMagnitude;

                // entity.motion は hire 中常時残留 false positive、distSq のみで判定
                // hysteresis：移動検知したら N 秒間 walk 維持（NPCCore の進む↔止まる細刻みを吸収）
                float now = Time.time;
                if (distSq > 0.0001f)
                {
                    lastMovingTime[entity.entityId] = now;
                }
                float lastT = lastMovingTime.ContainsKey(entity.entityId) ? lastMovingTime[entity.entityId] : -10f;
                bool isMoving = (now - lastT) < WALK_HOLD_SECONDS;
                anim.SetBool("IsWalking", isMoving);

                // 2秒に1回デバッグログ（state info も）
                float t = Time.time;
                if (!nextDebugLog.ContainsKey(entity.entityId) || t > nextDebugLog[entity.entityId])
                {
                    nextDebugLog[entity.entityId] = t + 2f;
                    var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                    Log.Out($"[WakaPet/Walk] id={entity.entityId} distSq={distSq:F6} motionSq={motionSq:F6} isMoving={isMoving} state={stateInfo.shortNameHash} t={stateInfo.normalizedTime:F2} speed={anim.speed:F2}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Force] Exception: {e}");
            }
        }
    }
}

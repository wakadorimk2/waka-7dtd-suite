using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// EntityAlive.Init 後に WakaPet entity の Animator 状態を Player.log にダンプする診断 patch.
    /// アニメ駆動失敗の真因（Animator の enabled / controller / avatar / SkinnedMesh の bones）を特定するため.
    /// 各 entityID あたり 1 回だけログ出して log を汚さない.
    /// </summary>
    /// <summary>
    /// EModelBase.LateUpdate の Postfix で初回 1 回だけ診断ログ.
    /// mesh + Animator が attach 完了したフレーム以降に走る.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class WakaPet_OnSpawned_Diagnostics
    {
        static HashSet<int> dumpedIds = new HashSet<int>();

        static void Postfix(EModelBase __instance)
        {
            try
            {
                if (__instance == null) return;
                var goInstance = __instance.gameObject;
                if (goInstance == null) return;
                var entity = goInstance.GetComponent<EntityAlive>();
                if (entity == null) return;
                var entityClass = EntityClass.list[entity.entityClass];
                if (entityClass == null) return;
                if (entityClass.entityClassName == null) return;
                if (!entityClass.entityClassName.StartsWith("entityWakaPet")) return;

                if (dumpedIds.Contains(entity.entityId)) return;
                dumpedIds.Add(entity.entityId);

                var __instanceRef = entity;
                var go = goInstance;
                Log.Out($"[WakaPet/Diag] === Entity '{entityClass.entityClassName}' id={entity.entityId} firstLateUpdate ===");
                Log.Out($"[WakaPet/Diag] go.name={go.name}, active={go.activeInHierarchy}");

                // 全 transform 階層を 1 段階ずつ dump
                Log.Out($"[WakaPet/Diag] === Hierarchy walk ===");
                DumpHierarchy(go.transform, 0, 4); // depth 4 まで

                // Animator 探索（include inactive）
                var animators = go.GetComponentsInChildren<Animator>(true);
                Log.Out($"[WakaPet/Diag] Animators: {animators.Length}");
                foreach (var a in animators)
                {
                    Log.Out($"[WakaPet/Diag]   Animator on '{a.gameObject.name}'");
                    Log.Out($"[WakaPet/Diag]     enabled={a.enabled}");
                    Log.Out($"[WakaPet/Diag]     runtimeAnimatorController={(a.runtimeAnimatorController != null ? a.runtimeAnimatorController.name : "NULL")}");
                    Log.Out($"[WakaPet/Diag]     avatar={(a.avatar != null ? a.avatar.name : "NULL")}");
                    Log.Out($"[WakaPet/Diag]     applyRootMotion={a.applyRootMotion}");
                    Log.Out($"[WakaPet/Diag]     hasTransformHierarchy={a.hasTransformHierarchy}");
                    Log.Out($"[WakaPet/Diag]     speed={a.speed}");
                    Log.Out($"[WakaPet/Diag]     cullingMode={a.cullingMode}");
                    Log.Out($"[WakaPet/Diag]     updateMode={a.updateMode}");
                    if (a.runtimeAnimatorController != null)
                    {
                        var clips = a.runtimeAnimatorController.animationClips;
                        Log.Out($"[WakaPet/Diag]     clips: {clips.Length}");
                        foreach (var c in clips)
                        {
                            Log.Out($"[WakaPet/Diag]       clip='{c.name}' length={c.length:F2}s");
                        }
                    }
                }

                // SkinnedMeshRenderer 探索
                var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                Log.Out($"[WakaPet/Diag] SkinnedMeshRenderers: {smrs.Length}");
                foreach (var smr in smrs)
                {
                    Log.Out($"[WakaPet/Diag]   SMR on '{smr.gameObject.name}'");
                    Log.Out($"[WakaPet/Diag]     enabled={smr.enabled}, mesh={(smr.sharedMesh != null ? smr.sharedMesh.name : "NULL")}");
                    Log.Out($"[WakaPet/Diag]     bones={smr.bones.Length}, rootBone={(smr.rootBone != null ? smr.rootBone.name : "NULL")}");
                    var bonesSample = smr.bones.Length > 0 ? smr.bones[0]?.name : "NULL";
                    Log.Out($"[WakaPet/Diag]     bones[0]={bonesSample}");
                }

                // EModelBase / avatarController（__instance は EModelBase）
                Log.Out($"[WakaPet/Diag] EModelBase: type={__instance.GetType().Name}");
                if (__instance.avatarController != null)
                {
                    Log.Out($"[WakaPet/Diag]   avatarController type={__instance.avatarController.GetType().FullName}");
                }
                else
                {
                    Log.Out($"[WakaPet/Diag]   avatarController=NULL");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Diag] Exception: {e}");
            }
        }

        static void DumpHierarchy(Transform t, int depth, int maxDepth)
        {
            if (depth > maxDepth) return;
            string indent = new string(' ', depth * 2);
            var components = t.GetComponents<Component>();
            var compNames = new System.Text.StringBuilder();
            foreach (var c in components)
            {
                if (c == null) continue;
                if (compNames.Length > 0) compNames.Append(",");
                compNames.Append(c.GetType().Name);
                // Renderer なら enabled も
                if (c is Renderer r)
                {
                    compNames.Append("(enabled=" + r.enabled + ")");
                }
                if (c is Behaviour b && !(c is Renderer))
                {
                    compNames.Append("(enabled=" + b.enabled + ")");
                }
            }
            Log.Out($"[WakaPet/Diag]   {indent}'{t.name}' [{compNames}]");
            for (int i = 0; i < t.childCount; i++)
            {
                DumpHierarchy(t.GetChild(i), depth + 1, maxDepth);
            }
        }
    }
}

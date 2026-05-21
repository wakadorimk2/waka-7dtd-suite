using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// caged Rocky 発見性向上のため、entity 周囲に薄いcyan glow を追加する.
    ///
    /// 動かない＋音だけだと暗いPOI内で気付きづらいため、Faraday cage の
    /// 電磁シールド (canon) を表現する形で点光源を attach.
    ///
    /// hire 後は light を消す (SCore の "Leader" cvar が 0以外になったら hired).
    /// caged は entity class swap されないため、glow attach状態を維持しつつ
    /// hire 状態を都度チェックして light の enabled を切り替える.
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class WakaPet_CagedGlow
    {
        // entityId → attach された Light
        static Dictionary<int, Light> attachedLights = new Dictionary<int, Light>();

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

                // 既に attach 済み: hire 状態を都度チェックして light on/off
                if (attachedLights.TryGetValue(entity.entityId, out var existingLight))
                {
                    if (existingLight == null)
                    {
                        attachedLights.Remove(entity.entityId);
                        return;
                    }
                    bool hired = IsHired(entity);
                    if (hired && existingLight.enabled)
                    {
                        existingLight.enabled = false;
                        Log.Out($"[WakaPet/Glow] entity {entity.entityId} hired, light disabled");
                    }
                    else if (!hired && !existingLight.enabled)
                    {
                        // hire 解除されたら復活 (Layer 2 / fire 解雇 等の対応)
                        existingLight.enabled = true;
                    }
                    return;
                }

                // 新規 attach (重複 add 防止)
                if (go.GetComponent<Light>() != null) return;

                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.4f, 0.7f, 1f); // 薄いcyan、エリディアン技術感
                light.intensity = 1.5f;
                light.range = 6f;
                light.shadows = LightShadows.None;

                attachedLights[entity.entityId] = light;
                Log.Out($"[WakaPet/Glow] caged glow attached to entity {entity.entityId}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Glow] Exception: {e}");
            }
        }

        static bool IsHired(EntityAlive entity)
        {
            try
            {
                if (entity?.Buffs == null) return false;
                // SCore は hire 完了時に entity の "Leader" cvar に player の entityId をセット
                float leader = entity.Buffs.GetCustomVar("Leader");
                return leader > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// v0.8 Question? motion + Caged 起動演出 trigger.
    ///
    /// SCore の dialog window が WakaPet に対して開いている間 questionActive=true、
    /// 閉じたら false に戻して ProceduralLocomotion に通知する。
    ///
    /// owner は EntityAliveSDX (npcMeleeTemplate ベース) → entityClassName が
    /// "entityWakaPet*" のときだけトリガー、それ以外の NPC への dialog は素通り。
    ///
    /// caged Rocky (entityWakaPetRabbit_caged) との dialog open 時には
    /// "I am Rocky. You are Brother." 起動演出 (19_long_explain) を再生する.
    /// caged は最初から awake状態で wander するため (sleeper restore 抑止のため)、
    /// destroy/spawn による entity 置換は廃止. hire は SCore の dialog flow に委譲.
    /// </summary>
    public static class WakaPet_QuestionDialogHook
    {
        // OnClose 時には CurrentOwner が既に null になりうるので、Open 時に控えておく
        static int currentEntityId = -1;

        // 撫で（dialog open）時の応答セリフ群
        static readonly string[] PETTED_KEYS = {
            "08_amaze_triple", "09_amaze_solo", "10_yes_double",
            "15_thanks", "17_good_double", "22_amaze_yes"
        };

        // 起動演出のセリフ（"I am Rocky. You are Brother." 原典シーン再現）
        const string AWAKENING_KEY = "19_long_explain";

        static bool IsWakaPet(EntityAlive owner)
        {
            if (owner == null) return false;
            var ec = EntityClass.list[owner.entityClass];
            return ec != null && ec.entityClassName != null
                && ec.entityClassName.StartsWith("entityWakaPet");
        }

        static bool IsCaged(EntityAlive owner)
        {
            if (owner == null) return false;
            var ec = EntityClass.list[owner.entityClass];
            return ec != null && ec.entityClassName == "entityWakaPetRabbit_caged";
        }

        [HarmonyPatch(typeof(XUiC_DialogWindowGroup), nameof(XUiC_DialogWindowGroup.OnOpen))]
        public class WakaPet_DialogOpen
        {
            public static void Postfix(XUiC_DialogWindowGroup __instance)
            {
                try
                {
                    var owner = __instance?.CurrentDialog?.CurrentOwner;
                    if (!IsWakaPet(owner)) return;
                    currentEntityId = owner.entityId;

                    if (IsCaged(owner))
                    {
                        // caged Rocky 起動演出: 19_long_explain ("I am Rocky. You are Brother.")
                        // entity destroy/spawn はしない (sleeper restore 補充ループ回避).
                        // hire は SCore dialog flow に委譲、entity はそのまま hire 可能.
                        WakaPetVoice.Play(AWAKENING_KEY, owner.gameObject);
                        Log.Out($"[WakaPet/Dialog] Caged Rocky awakening voice played, entityId={owner.entityId}");
                    }
                    else
                    {
                        // 通常 Waka Pet: 撫で応答セリフ + question motion
                        WakaPet_ProceduralLocomotion.SetQuestionActive(owner.entityId, true);
                        WakaPetVoice.PlayRandom(PETTED_KEYS, owner.gameObject);
                    }
                }
                catch (System.Exception e)
                {
                    Log.Error($"[WakaPet/Dialog] OnOpen postfix exception: {e}");
                }
            }
        }

        [HarmonyPatch(typeof(XUiC_DialogWindowGroup), nameof(XUiC_DialogWindowGroup.OnClose))]
        public class WakaPet_DialogClose
        {
            public static void Postfix()
            {
                try
                {
                    if (currentEntityId == -1) return;
                    WakaPet_ProceduralLocomotion.SetQuestionActive(currentEntityId, false);
                    currentEntityId = -1;
                }
                catch (System.Exception e)
                {
                    Log.Error($"[WakaPet/Dialog] OnClose postfix exception: {e}");
                }
            }
        }
    }
}

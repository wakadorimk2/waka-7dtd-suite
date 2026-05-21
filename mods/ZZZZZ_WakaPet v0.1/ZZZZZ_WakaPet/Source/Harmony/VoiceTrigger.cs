using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// v0.8 voice trigger. WakaPet entity の Update 毎 0.25s polling で
    /// idle / 敵接近 / 瀕死 を判定し、対応セリフを再生する。
    /// 撫で（dialog open）は WakaPet_QuestionDialogHook 側で再生。
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), "Update")]
    public static class WakaPet_VoiceTrigger
    {
        class Cooldowns
        {
            public float lastCheck;
            public float lastIdle;
            public float lastAlert;
            public float lastPain;
        }

        static readonly Dictionary<int, Cooldowns> states = new Dictionary<int, Cooldowns>();

        // 状況別セリフ key（Resources/voice/*.wav 拡張子無し）
        static readonly string[] IDLE_KEYS = {
            "02_curious", "07_question_solo", "12_sad", "13_understand",
            "16_eat_q", "17_good_double", "18_friend_solo", "21_sleep_q"
        };
        // caged 状態用: 「眠れる存在」感のある問いかけ・寝言系のみ
        // 発見性向上のため少し短いクールダウンで定期的に鳴らす
        static readonly string[] CAGED_IDLE_KEYS = {
            "21_sleep_q",      // "You sleep now, question?" - 寝言っぽい
            "12_sad",          // "Sad. Sad, statement." - 微弱な思考
            "07_question_solo" // "Question?" - 問いかけ
        };
        const string ALERT_KEY = "05_alert";
        const string PAIN_KEY  = "20_pain";

        const float CHECK_INTERVAL = 0.25f;
        const float IDLE_COOLDOWN  = 25f;
        const float IDLE_PROB      = 0.05f;  // 1 check (0.25s) ごとの発火確率
        const float CAGED_IDLE_COOLDOWN = 12f; // 短め: プレイヤーが気づきやすく
        const float CAGED_IDLE_PROB     = 0.08f; // 高め
        const float ALERT_COOLDOWN = 20f;
        const float ALERT_RADIUS   = 15f;
        const float PAIN_COOLDOWN  = 60f;
        const float PAIN_HP_RATIO  = 0.3f;

        // GetEntitiesInBounds 用の reuse list（毎フレーム new しない）
        static readonly List<global::Entity> tmpEntities = new List<global::Entity>(16);

        static bool IsWakaPet(EntityAlive e)
        {
            if (e == null) return false;
            var ec = EntityClass.list[e.entityClass];
            return ec != null && ec.entityClassName != null
                && ec.entityClassName.StartsWith("entityWakaPet");
        }

        static bool IsCaged(EntityAlive e)
        {
            if (e == null) return false;
            var ec = EntityClass.list[e.entityClass];
            return ec?.entityClassName == "entityWakaPetRabbit_caged";
        }

        public static void Postfix(EntityAlive __instance)
        {
            try
            {
                if (!IsWakaPet(__instance)) return;
                if (__instance.IsDead()) return;

                int id = __instance.entityId;
                float now = Time.time;

                if (!states.TryGetValue(id, out var cd))
                {
                    cd = new Cooldowns();
                    states[id] = cd;
                }

                if (now - cd.lastCheck < CHECK_INTERVAL) return;
                cd.lastCheck = now;

                var go = __instance.gameObject;

                // caged 専用: 寝言系セリフのみ低頻度。pain/alert なし（無敵で戦わない）
                if (IsCaged(__instance))
                {
                    if (now - cd.lastIdle > CAGED_IDLE_COOLDOWN)
                    {
                        if (Random.value < CAGED_IDLE_PROB)
                        {
                            WakaPetVoice.PlayRandom(CAGED_IDLE_KEYS, go);
                            cd.lastIdle = now;
                        }
                    }
                    return;
                }

                // 1. 瀕死 pain（最優先、他トリガを抑止）
                int hp = __instance.Health;
                int maxHp = __instance.GetMaxHealth();
                if (maxHp > 0 && (float)hp / maxHp < PAIN_HP_RATIO)
                {
                    if (now - cd.lastPain > PAIN_COOLDOWN)
                    {
                        WakaPetVoice.Play(PAIN_KEY, go);
                        cd.lastPain = now;
                        return;
                    }
                }

                // 2. 敵接近 alert
                if (now - cd.lastAlert > ALERT_COOLDOWN)
                {
                    if (HasNearbyEnemy(__instance, ALERT_RADIUS))
                    {
                        WakaPetVoice.Play(ALERT_KEY, go);
                        cd.lastAlert = now;
                        return;
                    }
                }

                // 3. idle 低確率
                if (now - cd.lastIdle > IDLE_COOLDOWN)
                {
                    if (Random.value < IDLE_PROB)
                    {
                        WakaPetVoice.PlayRandom(IDLE_KEYS, go);
                        cd.lastIdle = now;
                    }
                }
            }
            catch (System.Exception e)
            {
                if (Time.frameCount % 600 == 0)
                    Log.Warning($"[WakaPet/VoiceTrigger] sample exception: {e.Message}");
            }
        }

        static bool HasNearbyEnemy(EntityAlive self, float radius)
        {
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            if (world == null) return false;

            var pos = self.position;
            var bounds = new Bounds(pos, Vector3.one * radius * 2f);
            tmpEntities.Clear();
            world.GetEntitiesInBounds(typeof(EntityZombie), bounds, tmpEntities);

            float r2 = radius * radius;
            for (int i = 0; i < tmpEntities.Count; i++)
            {
                if (tmpEntities[i] is EntityAlive ea && !ea.IsDead())
                {
                    if ((ea.position - pos).sqrMagnitude <= r2) return true;
                }
            }
            return false;
        }
    }
}

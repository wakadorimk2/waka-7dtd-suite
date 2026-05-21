using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// v0.7.2 procedural locomotion for Rocky (Action Figure rig).
    ///
    /// Bone 構造（自前 12-bone rig、極点ベース配置）:
    ///   Pelvis (root, 体中央)
    ///   Body (Pelvis 子, look-at 用)
    ///     Arm_Upper → Arm_Lower (掲げ手, 右後上, 動かさない)
    ///   LegL_Upper → LegL_Lower (左下脚)
    ///   LegF_Upper → LegF_Lower (前下脚)
    ///   LegB_Upper → LegB_Lower (後ろ脚)
    ///   LegR_Upper → LegR_Lower (右下脚)
    ///
    /// Gait: 4本歩行肢、対角ペア同期 (LegL+LegR ↔ LegF+LegB の 2拍子)。
    /// 移動速度から step 周期を逆算、rest pose からの delta rotation で控えめに振る。
    /// </summary>
    [HarmonyPatch(typeof(EModelBase), "LateUpdate")]
    public class WakaPet_ProceduralLocomotion
    {
        class State
        {
            public Transform pelvis;
            public Vector3 restPelvisLocalPos;
            public Quaternion restPelvisLocalRot;
            public Leg[] legs;
            public Vector3 lastWorldPos;
            public float walkPhase;
            public float lastSpeed;
            public float walkAmpFactor;
            public bool initialized;
            public float nextDebugLog;
            public Gait gait;

            // Question? motion (dialog 中の 2-stage ポーズ：tilt → elevate)
            public bool questionActive;
            public float questionTime;  // sec, active 中は加算、inactive 中は早送り減算
            // tiltAmp = stage1 (0→1, 1秒)、liftAmp = stage2 (0→1, 0.3秒、stage1 完了後に発火)
            // 計算済み値、debug log 用にも保持
            public float tiltAmp;
            public float liftAmp;
            public Transform pelvisParent;        // Pelvis 親 = entity bonesRoot、IK target の参照軸
        }

        struct Leg
        {
            public string name;
            public Transform upper;
            public Transform lower;
            public Quaternion restUpperLocalRot;
            public Quaternion restLowerLocalRot;
            public Vector3 restUpperLocalPos;
            public int compass; // 0=F (前), 1=R (右), 2=B (後), 3=L (左) — 時計回り

            // Two-Bone IK 用
            public float upperLen;            // hip→knee 距離 (実測)
            public float lowerLen;            // knee→tip 距離 (子 bone なし、現状 upperLen と仮定)
            public Vector3 boneAxisLocal;     // upper の local 空間で knee へ向かう方向 (lower にも流用)
            public Vector3 restTipParentLocal; // rest 時の足先 tip を Pelvis 親 local で保存 (gait phase 非依存)
        }

        enum Gait { Wave, Trot }

        // gait ごとの脚 phase offset (compass index 0..3 = F, R, B, L)
        // Wave: 1 脚ずつ時計回り (F → R → B → L)
        // Trot: 対角ペア (F+L) ↔ (B+R)
        static readonly float[] WAVE_OFFSETS = new float[] {
            0f,                  // F
            Mathf.PI * 0.5f,     // R
            Mathf.PI,            // B
            Mathf.PI * 1.5f,     // L
        };
        static readonly float[] TROT_OFFSETS = new float[] {
            0f,                  // F  ──┐ 前左ペア
            Mathf.PI,            // R    │ 後右ペア
            Mathf.PI,            // B  ──┘
            0f,                  // L
        };

        static Dictionary<int, State> states = new Dictionary<int, State>();

        const float STEP_LENGTH         = 0.5f;
        const float WALK_SPEED_THRESHOLD = 0.05f;
        const float WALK_AMP_LERP       = 4.0f;
        const float DEBUG_LOG_INTERVAL  = 3.0f;
        const float SWING_ANGLE_MAX     = 25f;  // upper bone の前後振り (度)
        const float LIFT_ANGLE_MAX      = 20f;  // lower bone の追加曲げ (度)
        const float BOB_AMP             = 0.05f; // Pelvis 上下ボブ (m), 着地ごとに沈む
        const float PITCH_AMP           = 10f;  // 前後ピッチ (度), 前ペア/後ペアの lift で前後傾
        const float ROLL_AMP            = 4f;   // 左右ロール (度), L/R ペアの lift で左右傾
        const float TROT_BODY_MULT      = 2f;   // trot 時は body 揺動を更に倍化
        const float BODY_LERP           = 8f;   // body の追従速さ (急な姿勢変化を慣性で滑らかに)
        const float LEG_LERP            = 30f;  // leg bone の追従速さ (gait 切替時のジャンプ吸収)
        const float TROT_TRIGGER_UP     = 0.3f; // wave → trot に切替える速度閾値 (m/s)、ある程度動いたら trot
        const float TROT_TRIGGER_DOWN   = 0.12f; // trot → wave に戻す速度閾値 (ヒステリシス、ほぼ停止状態のみ wave)

        // Question? motion 定数（PHM Rocky 流の 2-stage シーケンス）
        // Stage1 (0-1s)  : 5脚接地 (Arm fold) + 胴体ゆっくり tilt
        // Stage2 (1-1.3s): 胴体一気に elevate
        // close 時は逆順で 0.4 秒で rest pose に戻る
        const float Q_STAGE1_DURATION        = 1.0f;  // 秒, tilt + arm fold 完了
        const float Q_STAGE2_DURATION        = 0.3f;  // 秒, elevate 完了 (一気に)
        const float Q_CLOSE_SPEED            = 3.0f;  // close 時の questionTime 減速倍率 (≈0.4秒で rest 復帰)
        const float QUESTION_TILT_AMP        = -15f;  // Body roll 静的角 (度), 首かしげ
        const float QUESTION_LIFT_AMP        = 0.13f; // Body 上昇 (m), Two-Bone IK で reach 限界に近い値
        // 脚先 world ピン留めは Two-Bone IK 実装 (Pelvis 更新後の IK ループで処理)
        const float QUESTION_PITCH_AMP       = 0f;    // 必要になったら使う pitch 静的オフセット (度)

        // idle 呼吸 (walking してなくても微小に上下、生命感＋procedural 動作確認用)
        const float IDLE_BREATH_FREQ_HZ      = 0.35f; // ≈2.9 秒 1 周期
        const float IDLE_BREATH_AMP          = 0.012f; // ±1.2cm

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

                if (!states.TryGetValue(entity.entityId, out var st))
                {
                    st = new State();
                    Initialize(st, rockyChild, entity.entityId);
                    states[entity.entityId] = st;
                }
                if (!st.initialized) return;

                Vector3 pelvisWorld = st.pelvis.position;
                Vector3 deltaPos = pelvisWorld - st.lastWorldPos;
                deltaPos.y = 0;
                float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                float instantSpeed = deltaPos.magnitude / dt;
                st.lastSpeed = Mathf.Lerp(st.lastSpeed, instantSpeed, dt * 6f);
                st.lastWorldPos = pelvisWorld;

                bool isWalking = st.lastSpeed > WALK_SPEED_THRESHOLD;
                float ampTarget = isWalking ? 1f : 0f;
                st.walkAmpFactor = Mathf.MoveTowards(st.walkAmpFactor, ampTarget, dt * WALK_AMP_LERP);

                float dynFreq = st.lastSpeed > 0.001f
                    ? st.lastSpeed / (STEP_LENGTH * 2f)
                    : 0f;
                dynFreq = Mathf.Clamp(dynFreq, 0f, 2.5f);

                if (st.walkAmpFactor > 0.001f && dynFreq > 0.001f)
                    st.walkPhase += dt * dynFreq * Mathf.PI * 2f;

                // gait 判定 (ヒステリシス付き)
                if (st.gait == Gait.Wave && st.lastSpeed > TROT_TRIGGER_UP) st.gait = Gait.Trot;
                else if (st.gait == Gait.Trot && st.lastSpeed < TROT_TRIGGER_DOWN) st.gait = Gait.Wave;
                var offsets = (st.gait == Gait.Trot) ? TROT_OFFSETS : WAVE_OFFSETS;

                // wave gait のときは 4 脚順次なので step 周波数を 1/2 に下げる (1 サイクルで 4脚分)
                // trot のときは 2 ペアで 1 サイクル = 既存どおり
                float legT = 1f - Mathf.Exp(-LEG_LERP * dt);

                for (int i = 0; i < st.legs.Length; i++)
                {
                    var leg = st.legs[i];

                    float phase = st.walkPhase + offsets[leg.compass];
                    float swing = Mathf.Sin(phase) * st.walkAmpFactor;
                    float lift  = Mathf.Max(0f, Mathf.Cos(phase)) * st.walkAmpFactor;

                    Quaternion upperTarget = leg.restUpperLocalRot * Quaternion.Euler(swing * SWING_ANGLE_MAX, 0f, 0f);
                    leg.upper.localRotation = Quaternion.Slerp(leg.upper.localRotation, upperTarget, legT);

                    if (leg.lower != null)
                    {
                        Quaternion lowerTarget = leg.restLowerLocalRot * Quaternion.Euler(lift * LIFT_ANGLE_MAX, 0f, 0f);
                        leg.lower.localRotation = Quaternion.Slerp(leg.lower.localRotation, lowerTarget, legT);
                    }
                }

                // 本体の bob / pitch / roll
                // bound gait: phase 0 = LR ペアが lift (cos+1), phase π = FB ペアが lift (cos-1→0、+1)
                //   - bob: 着地ごとに沈むので 2倍周期で |cos| 反転 → 重心ジャンプ感
                //   - pitch: 前後ペア lift 時に前後傾 (FB ペア lift = 後傾、LR ペアは前後成分無し)
                //   - roll: 左右ペア lift 時に左右傾 (LR ペア lift = 左に傾く感、ただし L/R は同位相なので相殺、cos2倍で揺れに変換)
                float amp = st.walkAmpFactor;
                float gaitMul = (st.gait == Gait.Trot) ? TROT_BODY_MULT : 1f;
                // bob: cos(2*phase) の平方根風 (足が地面についてる瞬間に沈む、振り上げ瞬間に上がる)
                float bobOffset = -Mathf.Abs(Mathf.Cos(st.walkPhase)) * BOB_AMP * amp * gaitMul;
                // pitch: FB ペア (phaseGroup=1) が lift max のとき後傾 (+pitch), LR ペアは pitch には効かない想定で sin(phase) 寄与
                float pitchAngle = Mathf.Sin(st.walkPhase + Mathf.PI * 0.5f) * PITCH_AMP * amp * gaitMul; // = cos(walkPhase)
                // roll: 左右の歩幅差で揺れる、phase 2倍で小刻みなロール
                float rollAngle = Mathf.Sin(st.walkPhase * 2f) * ROLL_AMP * amp * gaitMul;

                // === Question? motion (2-stage シーケンス) ===
                // questionTime: active 中は加算、inactive 中は close speed で 0 へ巻き戻し
                // stage1 で tilt + arm fold を完了、stage2 で一気に elevate
                if (st.questionActive)
                {
                    st.questionTime = Mathf.Min(st.questionTime + dt,
                                                Q_STAGE1_DURATION + Q_STAGE2_DURATION + 5f); // hold cap
                }
                else
                {
                    st.questionTime = Mathf.Max(0f, st.questionTime - dt * Q_CLOSE_SPEED);
                }

                // stage 1: tilt + arm fold (linear、ゆっくりかしげる)
                st.tiltAmp = Mathf.Clamp01(st.questionTime / Q_STAGE1_DURATION);
                // stage 2: elevate (smoothstep、stage1 完了直後に一気)
                float liftRaw = Mathf.Clamp01((st.questionTime - Q_STAGE1_DURATION) / Q_STAGE2_DURATION);
                st.liftAmp = liftRaw * liftRaw * (3f - 2f * liftRaw); // smoothstep

                // body かしげ
                rollAngle  += QUESTION_TILT_AMP  * st.tiltAmp;
                pitchAngle += QUESTION_PITCH_AMP * st.tiltAmp;

                // idle 呼吸 (walking してないとき有効、確認しやすく＋生命感)
                float idleBreath = Mathf.Sin(Time.time * 2f * Mathf.PI * IDLE_BREATH_FREQ_HZ)
                                   * IDLE_BREATH_AMP * (1f - st.walkAmpFactor);
                bobOffset += idleBreath;

                // === Pelvis 移動の合成 ===
                // walking bob と breathing は body 軸 (local Y)、Question elevate は world up に固定
                // Pelvis 親 (Body root) が tilt や entity rotation で傾いてるとき、
                // 単純な local Y 加算だと水平方向にも漏れて「バックステップ」してしまう。
                // world up を Pelvis 親 local 空間に変換して、世界座標で常に真上に lift する。
                float worldLiftMag = QUESTION_LIFT_AMP * st.liftAmp;
                Vector3 worldLiftLocal = (st.pelvis.parent != null)
                    ? st.pelvis.parent.InverseTransformDirection(Vector3.up) * worldLiftMag
                    : Vector3.up * worldLiftMag;

                Vector3 targetLocalPos = st.restPelvisLocalPos
                                       + new Vector3(0f, bobOffset, 0f)
                                       + worldLiftLocal;
                Quaternion targetLocalRot = st.restPelvisLocalRot * Quaternion.Euler(pitchAngle, 0f, rollAngle);

                // 慣性つきで適用 (急変ガード)
                float t = 1f - Mathf.Exp(-BODY_LERP * dt);
                st.pelvis.localPosition = Vector3.Lerp(st.pelvis.localPosition, targetLocalPos, t);
                st.pelvis.localRotation = Quaternion.Slerp(st.pelvis.localRotation, targetLocalRot, t);

                // 静止＆Question? 非active のとき、足先の rest 位置を pelvis 親 local で
                // 毎フレーム更新（gait phase に依存せず、最後に静止してた時の足先を IK target に保つ）
                if (st.walkAmpFactor < 0.01f && st.liftAmp < 0.01f && st.pelvisParent != null)
                {
                    for (int i = 0; i < st.legs.Length; i++)
                    {
                        var leg = st.legs[i];
                        if (leg.lower == null) continue;
                        Vector3 axisWorld = leg.lower.TransformDirection(leg.boneAxisLocal);
                        Vector3 tipWorld = leg.lower.position + axisWorld * leg.lowerLen;
                        st.legs[i].restTipParentLocal = st.pelvisParent.InverseTransformPoint(tipWorld);
                    }
                }

                // === Two-Bone IK (legs) ===
                // Pelvis lift 後、各脚の tip を rest 位置にピン留めするよう upper/lower の rotation を逆算
                // weight = liftAmp で smoothly fade in/out。pelvisParent の transform で entity 移動に追従。
                if (st.liftAmp > 0.001f && st.pelvisParent != null)
                {
                    for (int i = 0; i < st.legs.Length; i++)
                    {
                        var leg = st.legs[i];
                        if (leg.lower == null || leg.upperLen < 1e-4f) continue;

                        Vector3 target = st.pelvisParent.TransformPoint(leg.restTipParentLocal);
                        SolveTwoBoneIK(leg.upper, leg.lower, leg.upperLen, leg.lowerLen,
                                       leg.boneAxisLocal, target, leg.lower.position,
                                       out var ikUpperRot, out var ikLowerRot);
                        leg.upper.rotation = Quaternion.Slerp(leg.upper.rotation, ikUpperRot, st.liftAmp);
                        leg.lower.rotation = Quaternion.Slerp(leg.lower.rotation, ikLowerRot, st.liftAmp);
                    }
                }

                // Arm は触らない (rest 上向きの掲げ姿勢キープ、Pelvis tilt は自動で cascade)

                if (Time.time > st.nextDebugLog)
                {
                    st.nextDebugLog = Time.time + DEBUG_LOG_INTERVAL;
                    Log.Out($"[WakaPet/Proc] id={entity.entityId} gait={st.gait} speed={st.lastSpeed:F2} amp={st.walkAmpFactor:F2} freq={dynFreq:F2} phase={(st.walkPhase % (Mathf.PI * 2)):F2} legs={st.legs.Length} qT={st.questionTime:F2} tilt={st.tiltAmp:F2} lift={st.liftAmp:F2}{(st.questionActive ? "*" : "")}");
                }
            }
            catch (System.Exception e)
            {
                Log.Error($"[WakaPet/Proc] Exception: {e}");
            }
        }

        static void Initialize(State st, Transform rockyChild, int entityId)
        {
            st.pelvis = FindBone(rockyChild, "Pelvis");
            if (st.pelvis == null)
            {
                Log.Warning($"[WakaPet/Proc] entity {entityId}: Pelvis not found");
                return;
            }
            st.restPelvisLocalPos = st.pelvis.localPosition;
            st.restPelvisLocalRot = st.pelvis.localRotation;

            var legList = new List<Leg>();
            void TryAddLeg(string label, string upperN, string lowerN, int compass)
            {
                var u = FindBone(rockyChild, upperN);
                var l = FindBone(rockyChild, lowerN);
                if (u == null)
                {
                    Log.Warning($"[WakaPet/Proc] missing bone for leg '{label}': upper={upperN}");
                    return;
                }
                // IK 用 bone 計測 (lower がある時だけ意味があるが、無い場合も safe な値を入れる)
                float upperLen = 0f;
                Vector3 boneAxisLocal = Vector3.down; // fallback
                if (l != null)
                {
                    upperLen = (l.position - u.position).magnitude;
                    Vector3 localKnee = u.InverseTransformPoint(l.position);
                    if (localKnee.sqrMagnitude > 1e-8f) boneAxisLocal = localKnee.normalized;
                }
                float lowerLen = upperLen; // 子 bone がないので仮定 (視覚調整可能)

                // rest pose tip world 位置 (gait phase 非依存の IK target、Pelvis 親 local で保存)
                Vector3 restTipParentLocal = Vector3.zero;
                if (l != null && st.pelvis.parent != null)
                {
                    Vector3 axisWorld = l.TransformDirection(boneAxisLocal);
                    Vector3 tipWorld = l.position + axisWorld * lowerLen;
                    restTipParentLocal = st.pelvis.parent.InverseTransformPoint(tipWorld);
                }

                legList.Add(new Leg
                {
                    name = label,
                    upper = u,
                    lower = l,
                    restUpperLocalRot = u.localRotation,
                    restLowerLocalRot = l != null ? l.localRotation : Quaternion.identity,
                    restUpperLocalPos = u.localPosition,
                    compass = compass,
                    upperLen = upperLen,
                    lowerLen = lowerLen,
                    boneAxisLocal = boneAxisLocal,
                    restTipParentLocal = restTipParentLocal,
                });
                Log.Out($"[WakaPet/Proc] leg '{label}' compass={compass}: upper={upperN}({u != null}) lower={lowerN}({l != null}) upperLen={upperLen:F3} axis={boneAxisLocal} restTipParentLocal={restTipParentLocal}");
            }

            // 時計回り (上から見て): F=0 → R=1 → B=2 → L=3
            TryAddLeg("F", "LegF_Upper", "LegF_Lower", 0);
            TryAddLeg("R", "LegR_Upper", "LegR_Lower", 1);
            TryAddLeg("B", "LegB_Upper", "LegB_Lower", 2);
            TryAddLeg("L", "LegL_Upper", "LegL_Lower", 3);

            st.gait = Gait.Wave; // 初期 gait は wave (ゆっくり)

            // Arm は触らない (rest 上向きキープ、Pelvis tilt は階層継承で自動追従)
            // Pelvis 親 (entity bonesRoot) を保存、leg IK target の参照軸として使用
            st.pelvisParent = st.pelvis.parent;
            Log.Out($"[WakaPet/Proc] pelvisParent={(st.pelvisParent != null ? st.pelvisParent.name : "null")}");

            st.legs = legList.ToArray();
            st.lastWorldPos = st.pelvis.position;
            st.walkPhase = 0f;
            st.walkAmpFactor = 0f;
            st.initialized = (st.legs.Length > 0);

            Log.Out($"[WakaPet/Proc] entity {entityId} init done: legs={st.legs.Length}");
        }

        // === Two-Bone IK helper ===
        // 2本骨を target に届くよう逆運動学で解いて、希望する upper/lower world 回転を返す
        // poleHint は bend 方向のヒント、weight blend は呼び出し側で行う
        static void SolveTwoBoneIK(
            Transform upper, Transform lower,
            float upperLen, float lowerLen,
            Vector3 boneAxisLocal,
            Vector3 target, Vector3 poleHint,
            out Quaternion ikUpperRot, out Quaternion ikLowerRot)
        {
            Vector3 hip = upper.position;
            Vector3 chord = target - hip;
            float chordMag = chord.magnitude;
            if (chordMag < 1e-4f || upperLen < 1e-4f)
            {
                ikUpperRot = upper.rotation;
                ikLowerRot = lower.rotation;
                return;
            }

            float minLen = Mathf.Abs(upperLen - lowerLen) + 1e-3f;
            float maxLen = upperLen + lowerLen - 1e-3f;
            float chordLen = Mathf.Clamp(chordMag, minLen, maxLen);
            Vector3 chordDir = chord / chordMag;

            float u2 = upperLen * upperLen;
            float l2 = lowerLen * lowerLen;
            float c2 = chordLen * chordLen;
            float hipCos = (u2 + c2 - l2) / (2f * upperLen * chordLen);
            hipCos = Mathf.Clamp(hipCos, -1f, 1f);
            float hipAngleRad = Mathf.Acos(hipCos);

            Vector3 hipToPole = poleHint - hip;
            Vector3 polePerp = Vector3.ProjectOnPlane(hipToPole, chordDir);
            if (polePerp.sqrMagnitude < 1e-6f)
            {
                polePerp = Vector3.ProjectOnPlane(Vector3.up, chordDir);
                if (polePerp.sqrMagnitude < 1e-6f)
                    polePerp = Vector3.ProjectOnPlane(Vector3.right, chordDir);
            }
            Vector3 poleDir = polePerp.normalized;
            Vector3 bendNormal = Vector3.Cross(chordDir, poleDir).normalized;

            Vector3 upperDir = Quaternion.AngleAxis(-hipAngleRad * Mathf.Rad2Deg, bendNormal) * chordDir;
            if (Vector3.Dot(upperDir, poleDir) < 0f)
                upperDir = Quaternion.AngleAxis(hipAngleRad * Mathf.Rad2Deg, bendNormal) * chordDir;
            Vector3 kneePos = hip + upperDir * upperLen;
            Vector3 lowerDir = (target - kneePos).normalized;

            Vector3 currentUpperAxisWorld = upper.TransformDirection(boneAxisLocal);
            Quaternion upperDelta = Quaternion.FromToRotation(currentUpperAxisWorld, upperDir);
            ikUpperRot = upperDelta * upper.rotation;

            Vector3 currentLowerAxisWorld = lower.TransformDirection(boneAxisLocal);
            Vector3 lowerAxisAfterUpper = upperDelta * currentLowerAxisWorld;
            Quaternion lowerDelta = Quaternion.FromToRotation(lowerAxisAfterUpper, lowerDir);
            ikLowerRot = lowerDelta * upperDelta * lower.rotation;
        }

        // 外部 (Dialog hook 等) から question motion を flip するためのエントリポイント
        // IK target は restTipParentLocal を毎フレーム参照する仕組みなので capture 不要
        public static void SetQuestionActive(int entityId, bool active)
        {
            if (states.TryGetValue(entityId, out var st))
            {
                st.questionActive = active;
                Log.Out($"[WakaPet/Proc] SetQuestionActive id={entityId} active={active} (questionTime={st.questionTime:F2})");
            }
            else
            {
                Log.Warning($"[WakaPet/Proc] SetQuestionActive id={entityId}: state not yet initialized, ignored");
            }
        }

        static Transform FindBone(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var c = FindBone(root.GetChild(i), name);
                if (c != null) return c;
            }
            return null;
        }
    }
}

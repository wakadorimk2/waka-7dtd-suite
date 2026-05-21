using System.Reflection;
using TMPro;
using UnityEngine;

namespace WakaDamageNumbersBoost
{
    // === Case B / C / D / E ===
    // MonoBehaviour attached to every Floating-class floaty by SetupTextMeshProBooster.
    //
    //   Case B (no flags)       : scale 0.3 -> 1.0 ease-out-back pop-in, hold,
    //                             shrink to 0 at lifetime tail.
    //   Case C (Crit)           : scale 0 -> 1.5 -> 1.0 elastic overshoot, lateral
    //                             scale jitter, brief red->fill flash, 1.5x
    //                             FloatingDamageNumber.floatSpeed boost.
    //   Case D (Fatal, no Crit) : scale 0 -> 1.4 -> 1.0 heavy slow bloom (0.35s),
    //                             low-frequency throb during the sustain phase.
    //   Case E (HS only)        : scale 0.3 -> 1.0 ease-out-back + a horizontal
    //                             stretch transient (scaleX peaks 1.4 then settles).
    //
    // All cases (including B) get a universal screen-x sin sway in LateUpdate so
    // dense damage events feel chatty like a DoS game's hit feed. Amplitude scales
    // with flag priority and damage tier.
    //
    // Self-disables on Start if any of the mod's bespoke animation components are
    // attached (Zapping / Zooming / Shotgun / Explosive / Wispy / Dripping) so we
    // don't fight them over transform.localScale.
    public class WakaFloatyBoost : MonoBehaviour
    {
        public bool IsCritical;
        public bool IsHeadshot;
        public bool IsFatal;
        public int DamageTier; // 0..4
        public Color MainColor;

        // Pop-in durations
        private const float POP_DUR = 0.15f;
        private const float CRIT_POP_DUR = 0.25f;
        private const float HS_POP_DUR = 0.18f;
        private const float FATAL_POP_DUR = 0.35f;

        // Shrink tail
        private const float SHRINK_TAIL = 0.15f;
        private const float FATAL_SHRINK_TAIL = 0.22f;

        // Color flash
        private const float CRIT_FLASH_DUR = 0.10f;

        // Crit jitter
        private const float CRIT_JITTER_AMP = 0.06f;
        private const float CRIT_JITTER_DECAY = 0.30f;
        private const float CRIT_SPEED_MULT = 1.5f;

        // Fatal throb
        private const float FATAL_THROB_FREQ = 6f;
        private const float FATAL_THROB_AMP = 0.04f;

        // HS stretch transient
        private const float HS_STRETCH_START = 0.05f;
        private const float HS_STRETCH_PEAK = 0.10f;
        private const float HS_STRETCH_END = 0.20f;
        private const float HS_STRETCH_X_PEAK = 1.4f;

        // Universal screen-x sway
        private const float SWAY_AMP_BASE = 0.04f;
        private const float SWAY_FREQ_BASE = 2.5f;

        private float _startTime;
        private Vector3 _baseScale;
        private bool _active;
        private TextMeshPro _tmp;
        private MonoBehaviour _fdn;
        private float _lifetime = 1.2f;

        private float _swayPhase;
        private float _swayFreq;
        private bool _swayBaseCaptured;
        private float _swayBaseX;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _tmp = GetComponent<TextMeshPro>();
            _swayPhase = Random.value * Mathf.PI * 2f;
            _swayFreq = SWAY_FREQ_BASE * (0.85f + Random.value * 0.3f);
        }

        private void Start()
        {
            if (HasOtherAnim())
            {
                Object.Destroy(this);
                return;
            }

            _active = true;
            _startTime = Time.time;

            if (TypeCache.FloatingDamageNumberType != null)
            {
                _fdn = GetComponent(TypeCache.FloatingDamageNumberType) as MonoBehaviour;
                if (_fdn != null)
                {
                    if (TypeCache.FloatingLifetimeField != null)
                    {
                        try { _lifetime = (float)TypeCache.FloatingLifetimeField.GetValue(_fdn); }
                        catch { /* keep default */ }
                    }
                    if (IsCritical && TypeCache.FloatingFloatSpeedField != null)
                    {
                        try
                        {
                            float cur = (float)TypeCache.FloatingFloatSpeedField.GetValue(_fdn);
                            TypeCache.FloatingFloatSpeedField.SetValue(_fdn, cur * CRIT_SPEED_MULT);
                        }
                        catch { /* ignore */ }
                    }
                }
            }

            // Initial scale: Crit/Fatal start tiny (more dramatic bloom).
            if (IsCritical || IsFatal)
            {
                transform.localScale = Vector3.zero;
            }
            else
            {
                transform.localScale = _baseScale * 0.3f;
            }
        }

        private bool HasOtherAnim()
        {
            return HasComponent(TypeCache.ZappingDamageNumberType)
                || HasComponent(TypeCache.ZoomingDamageNumberType)
                || HasComponent(TypeCache.ShotgunDamageNumberType)
                || HasComponent(TypeCache.ExplosiveDamageNumberType)
                || HasComponent(TypeCache.WispyDamageNumberType)
                || HasComponent(TypeCache.DrippingDamageNumberType);
        }

        private bool HasComponent(System.Type t)
        {
            if (t == null) return false;
            return GetComponent(t) != null;
        }

        private void Update()
        {
            if (!_active) return;

            float t = Time.time - _startTime;

            float popDur = ChoosePopDur();
            float shrinkTail = IsFatal && !IsCritical ? FATAL_SHRINK_TAIL : SHRINK_TAIL;

            // ===== Scale animation =====
            float scaleMult = ComputeScaleMult(t, popDur, shrinkTail);

            // ===== Crit-only lateral scale jitter (shake-like, no position drift) =====
            float jitterX = 1f, jitterY = 1f;
            if (IsCritical)
            {
                float sinceSettle = t - CRIT_POP_DUR;
                if (sinceSettle > 0f && sinceSettle < CRIT_JITTER_DECAY)
                {
                    float decay = 1f - sinceSettle / CRIT_JITTER_DECAY;
                    jitterX = 1f + Mathf.Sin(t * 60f) * CRIT_JITTER_AMP * decay;
                    jitterY = 1f + Mathf.Sin(t * 67f + 1.3f) * CRIT_JITTER_AMP * decay;
                }
            }

            // ===== Fatal-only throb during sustain =====
            if (IsFatal && !IsCritical && t > popDur && t < _lifetime - shrinkTail)
            {
                float sinceSustain = t - popDur;
                float throbDecay = Mathf.Clamp01(1f - sinceSustain / 0.6f);
                float throb = 1f + Mathf.Sin(sinceSustain * FATAL_THROB_FREQ) * FATAL_THROB_AMP * throbDecay;
                jitterX *= throb;
                jitterY *= throb;
            }

            // ===== HS stretch transient (horizontal burst on impact) =====
            if (IsHeadshot && !IsCritical)
            {
                if (t >= HS_STRETCH_START && t < HS_STRETCH_END)
                {
                    float stretchT;
                    if (t < HS_STRETCH_PEAK)
                    {
                        stretchT = (t - HS_STRETCH_START) / (HS_STRETCH_PEAK - HS_STRETCH_START);
                        jitterX *= Mathf.Lerp(1f, HS_STRETCH_X_PEAK, stretchT);
                        jitterY *= Mathf.Lerp(1f, 0.85f, stretchT);
                    }
                    else
                    {
                        stretchT = (t - HS_STRETCH_PEAK) / (HS_STRETCH_END - HS_STRETCH_PEAK);
                        jitterX *= Mathf.Lerp(HS_STRETCH_X_PEAK, 1f, stretchT);
                        jitterY *= Mathf.Lerp(0.85f, 1f, stretchT);
                    }
                }
            }

            transform.localScale = new Vector3(
                _baseScale.x * scaleMult * jitterX,
                _baseScale.y * scaleMult * jitterY,
                _baseScale.z * scaleMult);

            // ===== Color flash for Crit/HS (white -> main red, preserves FDN alpha) =====
            if ((IsCritical || IsHeadshot) && _tmp != null && t < CRIT_FLASH_DUR)
            {
                float p = t / CRIT_FLASH_DUR;
                Color flashCol = Color.Lerp(Color.white, MainColor, p);
                flashCol.a = _tmp.color.a;
                _tmp.color = flashCol;
            }

            if (t >= _lifetime)
            {
                Object.Destroy(this);
            }
        }

        private float ChoosePopDur()
        {
            if (IsCritical) return CRIT_POP_DUR;
            if (IsFatal) return FATAL_POP_DUR;
            if (IsHeadshot) return HS_POP_DUR;
            return POP_DUR;
        }

        private float ComputeScaleMult(float t, float popDur, float shrinkTail)
        {
            if (t < popDur)
            {
                float p = t / popDur;
                if (IsCritical)
                {
                    // 0 -> 1.5 linear for first 40%, then 1.5 -> 1.0 ease-out-back.
                    if (p < 0.4f) return Mathf.Lerp(0f, 1.5f, p / 0.4f);
                    return Mathf.Lerp(1.5f, 1.0f, EaseOutBack((p - 0.4f) / 0.6f));
                }
                if (IsFatal)
                {
                    // 0 -> 1.4 -> 1.0 heavy slow bloom.
                    if (p < 0.5f) return Mathf.Lerp(0f, 1.4f, EaseOutBack(p / 0.5f));
                    return Mathf.Lerp(1.4f, 1.0f, (p - 0.5f) / 0.5f);
                }
                // Base / HS: 0.3 -> 1.0 ease-out-back.
                return Mathf.Lerp(0.3f, 1.0f, EaseOutBack(p));
            }
            if (t > _lifetime - shrinkTail)
            {
                float p = (t - (_lifetime - shrinkTail)) / shrinkTail;
                return Mathf.Lerp(1.0f, 0f, Mathf.Clamp01(p));
            }
            return 1.0f;
        }

        private void LateUpdate()
        {
            if (!_active) return;

            // Capture initial world x once (after FDN.Start runs).
            // FDN updates position.y per-frame via "+= up * speed * dt", but leaves
            // x/z alone; we keep the same x but overlay a small screen-horizontal
            // (approximated as world-x) oscillation for the "DoS bustle" feeling.
            if (!_swayBaseCaptured)
            {
                _swayBaseX = transform.position.x;
                _swayBaseCaptured = true;
            }

            float t = Time.time - _startTime;
            float ampMult = 1f;
            if (IsCritical) ampMult = 2.5f;
            else if (IsHeadshot) ampMult = 1.6f;
            else if (IsFatal) ampMult = 1.8f;
            // Tier add (0..4 -> 0.0..0.6 extra)
            ampMult += DamageTier * 0.15f;

            float dx = Mathf.Sin(t * _swayFreq + _swayPhase) * SWAY_AMP_BASE * ampMult;
            Vector3 p = transform.position;
            p.x = _swayBaseX + dx;
            transform.position = p;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float t1 = t - 1f;
            return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
        }
    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace WakaDamageNumbersBoost
{
    // === Case A === Postfix on DamageController.SetupTextMeshPro to rewrite
    // TextMeshPro outline / fontSize / fontStyle / color / text based on the
    // in-flight pending entry. PendingTracker provides IsCritical / IsHeadshot
    // / IsFatal / DamageDealt.
    //
    // v0.2 changes:
    //  - HS/Crit color: hard-override to crimson red so bleed/proc-tinted hits
    //    don't bury the "this is a crit" signal in damage-type purple/orange.
    //  - Tier amplification: numeric damage tier (0..4) cross-cuts Crit/HS/Fatal,
    //    each step adds size + outline so 1000+ floaties are visibly heavier.
    //  - K format: 1000+ shown as "1.2K", 1M+ as "1.2M" for readability.
    //  - ZTest Always: per-font cached material variant, so floaties pop
    //    through walls and doors (vital for molotov-through-window cases).
    //  - WakaFloatyBoost attached for animation/jitter/trajectory in case B/C.
    [HarmonyPatch]
    public static class SetupTextMeshProBooster
    {
        // Cache one ZTest=Always material variant per source font material.
        // Damage Numbers spawns floaties rapidly; cloning per-floaty would burn
        // GC and instance count. One variant per font asset is enough since
        // we only swap the floaty's material reference, not the shared asset.
        private static readonly Dictionary<Material, Material> _zTestAlwaysCache = new Dictionary<Material, Material>();

        // TMP 3D shader exposes ZTest as `ZTest [unity_GUIZTestMode]`, so we set
        // that material property to 8 (UnityEngine.Rendering.CompareFunction.Always).
        // PropertyToID returns a stable hash safe to cache across the session.
        private static readonly int _zTestModePropId = Shader.PropertyToID("unity_GUIZTestMode");

        public static bool Prepare()
        {
            if (!TypeCache.Ready) TypeCache.Init();
            return TypeCache.Ready && TypeCache.SetupTextMeshProMethod != null;
        }

        public static MethodBase TargetMethod()
        {
            return TypeCache.SetupTextMeshProMethod;
        }

        public static void Postfix(TextMeshPro __result, GameObject go, Color color)
        {
            try
            {
                if (__result == null || go == null) return;

                bool isCrit = PendingTracker.IsCritical;
                bool isHS = PendingTracker.IsHeadshot;
                bool isFatal = PendingTracker.IsFatal;
                int dmg = PendingTracker.DamageDealt;

                // ===== Tier (0..4) by raw damage magnitude =====
                int tier = 0;
                if (dmg >= 1000) tier = 4;
                else if (dmg >= 500) tier = 3;
                else if (dmg >= 200) tier = 2;
                else if (dmg >= 50) tier = 1;

                // ===== Outline width =====
                // base 0.2, +crit/HS/fatal flag bonuses, +tier bonus stacked.
                float ow = 0.2f;
                if (isCrit) ow += 0.1f;
                if (isHS) ow += 0.1f;
                if (isFatal) ow += 0.05f;
                ow += tier * 0.04f;
                if (ow > 0.6f) ow = 0.6f;
                __result.outlineWidth = ow;

                // ===== Fill color =====
                // HS or Crit hard-override to crimson so bleed-procs (purple) and
                // similar damage-type colors don't drown out the crit signal.
                // Preserve the alpha that the original SetupTextMeshPro set.
                Color fill;
                if (isCrit || isHS)
                {
                    fill = new Color(1f, 0.18f, 0.12f, __result.color.a);
                    __result.color = fill;
                }
                else
                {
                    fill = __result.color;
                }

                // ===== Outline color =====
                // Crit/HS: deep maroon rim so the red fill reads as bloody.
                // Otherwise: dark version of the original fill (preserve hue).
                Color outlineCol;
                if (isCrit || isHS)
                {
                    outlineCol = new Color(0.35f, 0.02f, 0.02f, 1f);
                }
                else
                {
                    outlineCol = new Color(color.r * 0.25f, color.g * 0.25f, color.b * 0.25f, 1f);
                }
                __result.outlineColor = outlineCol;

                // ===== Font size =====
                // crit/HS/fatal stack multiplicatively, then tier bumps on top.
                float sizeMult = 1f;
                if (isCrit) sizeMult *= 1.15f;
                if (isHS) sizeMult *= 1.20f;
                if (isFatal) sizeMult *= 1.30f;
                sizeMult *= (1f + tier * 0.08f);
                if (sizeMult > 1.0001f)
                {
                    __result.fontSize *= sizeMult;
                }

                // ===== Font style =====
                // Bold for crit/HS, italic when both stack, underline for fatal,
                // strikethrough for fatal+crit (kill-with-overkill marker).
                FontStyles style = FontStyles.Normal;
                if (isCrit || isHS) style |= FontStyles.Bold;
                if (isCrit && isHS) style |= FontStyles.Italic;
                if (isFatal) style |= FontStyles.Underline;
                if (isFatal && (isCrit || isHS)) style |= FontStyles.Italic;
                if (style != FontStyles.Normal)
                {
                    __result.fontStyle = style;
                }

                // ===== Character spacing =====
                // Loud floaties get a slight letter spread for "heavy" feel.
                if (isCrit || (isFatal && tier >= 2))
                {
                    __result.characterSpacing = 8f;
                }

                // ===== K format =====
                // The original SetupTextMeshPro already wrote text. Rewrite if
                // we have a numeric value worth condensing.
                if (dmg >= 1000)
                {
                    __result.text = FormatLargeNumber(dmg);
                }

                // ===== ZTest Always (wall-through visibility) =====
                ApplyZTestAlways(__result);

                // ===== Attach animation booster =====
                if (PendingTracker.HasActivePending)
                {
                    var boost = go.AddComponent<WakaFloatyBoost>();
                    boost.IsCritical = isCrit;
                    boost.IsHeadshot = isHS;
                    boost.IsFatal = isFatal;
                    boost.DamageTier = tier;
                    // Pass the effective fill color (post-override) so the flash
                    // returns to red rather than the original purple/orange.
                    boost.MainColor = fill;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[WakaDamageNumbersBoost] SetupTextMeshProBooster.Postfix: " + e);
            }
        }

        private static string FormatLargeNumber(int n)
        {
            if (n < 1000) return n.ToString();
            if (n < 10000)
            {
                // 1.0K - 9.9K
                float k = n / 1000f;
                return k.ToString("0.0") + "K";
            }
            if (n < 1000000)
            {
                // 10K - 999K
                int k = n / 1000;
                return k.ToString() + "K";
            }
            // 1.0M+
            float m = n / 1000000f;
            return m.ToString("0.0") + "M";
        }

        private static void ApplyZTestAlways(TextMeshPro tmp)
        {
            try
            {
                var src = tmp.fontSharedMaterial;
                if (src == null) return;

                if (!_zTestAlwaysCache.TryGetValue(src, out var variant) || variant == null)
                {
                    variant = new Material(src);
                    // 8 = UnityEngine.Rendering.CompareFunction.Always.
                    // TMP 3D shader pass uses `ZTest [unity_GUIZTestMode]`, so
                    // overriding this material property forces always-pass.
                    variant.SetFloat(_zTestModePropId, 8f);
                    // Push render queue past Overlay range only mildly to keep
                    // floaties above world geo while remaining below proper UI.
                    variant.renderQueue = 4000;
                    _zTestAlwaysCache[src] = variant;
                }

                tmp.fontSharedMaterial = variant;
            }
            catch (Exception e)
            {
                Log.Warning("[WakaDamageNumbersBoost] ApplyZTestAlways: " + e);
            }
        }
    }
}

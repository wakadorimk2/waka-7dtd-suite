using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamAffixCurve
{
    /// <summary>
    /// PoE2-style quality curve for RAM affix slots.
    ///
    /// Vanilla RAM derives slot count from item attachment-slot count, so a
    /// q1 AR15 (which has many attachment slots) rolls the same number of
    /// affixes as a q6 one. We override the cap to follow item.Quality:
    ///   q1=1, q2=2, q3=3, q4=4, q5=5, q6=6 (hard ceiling).
    /// Magic Find perk pushes the cap by floor(level/2):
    ///   Lv0-1=+0, Lv2-3=+1, Lv4-5=+2 (still capped at 6 total).
    /// </summary>
    [HarmonyPatch]
    public static class CountModsToApplyPatch
    {
        const int HardCap = 6;

        public static bool Prepare(MethodBase _) => RamCurveBridge.Ready;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamCurveBridge.Ready) yield break;
            yield return RamCurveBridge.CountModsToApplyMethod;
        }

        public static void Postfix(ItemValue itemValue, EntityPlayer player, ref int __result)
        {
            try
            {
                if (itemValue == null || itemValue.IsEmpty()) return;
                int quality = itemValue.Quality;
                if (quality < 1) return;

                int mfLevel = 0;
                try
                {
                    var entAlive = (EntityAlive)player;
                    if (entAlive != null && entAlive.Progression != null)
                    {
                        mfLevel = entAlive.Progression.GetProgressionValue("perkMagicFind").level;
                    }
                }
                catch (Exception)
                {
                    // Player progression unavailable (e.g. non-player loot spawn); treat as Lv0
                }

                int mfBonus = mfLevel / 2; // Lv0-1=0, Lv2-3=1, Lv4-5=2
                int cap = quality + mfBonus;
                if (cap > HardCap) cap = HardCap;
                if (cap < 1) cap = 1;

                int alreadyApplied = 0;
                if (itemValue.CosmeticMods != null)
                {
                    for (int i = 0; i < itemValue.CosmeticMods.Length; i++)
                    {
                        var v = itemValue.CosmeticMods[i];
                        if (v != null && !v.IsEmpty() && v.ItemClass != null
                            && RamCurveBridge.IsAffixMod(v.ItemClass))
                        {
                            alreadyApplied++;
                        }
                    }
                }

                int newRemaining = cap - alreadyApplied;
                if (newRemaining < 0) newRemaining = 0;

                // Only narrow the result. Vanilla may already return fewer
                // (e.g. an item without attachment slots), in which case we
                // don't expand — that would require additional array allocation
                // and players didn't ask for affixes on slot-less items.
                if (newRemaining < __result)
                {
                    __result = newRemaining;

                    // Trim CosmeticMods array to the curve cap if safe — keeps
                    // tooltips honest (no phantom empty slots) and prevents
                    // future reroll/extraction from filling slots beyond cap.
                    if (itemValue.CosmeticMods != null && itemValue.CosmeticMods.Length > cap)
                    {
                        bool safeToTrim = true;
                        for (int i = cap; i < itemValue.CosmeticMods.Length; i++)
                        {
                            var v = itemValue.CosmeticMods[i];
                            if (v != null && !v.IsEmpty())
                            {
                                safeToTrim = false;
                                break;
                            }
                        }
                        if (safeToTrim)
                        {
                            var trimmed = new ItemValue[cap];
                            Array.Copy(itemValue.CosmeticMods, trimmed, cap);
                            itemValue.CosmeticMods = trimmed;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"[WakaRamAffixCurve] Postfix failed: {e.Message}");
            }
        }
    }
}

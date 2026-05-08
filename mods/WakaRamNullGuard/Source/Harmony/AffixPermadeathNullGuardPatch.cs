using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Hooked from EntityEnemy.DamageEntity Prefix. RAM calls
    /// `_damageSource.getEntityId()` before any null check, then later touches
    /// `_damageSource.AttackingItem`. Bail with __result=0f.
    /// </summary>
    [HarmonyPatch]
    public static class AffixPermadeathNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.PermadeathReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.PermadeathReady) yield break;
            yield return RamBridge.PermadeathCheckMethod;
        }

        public static bool Prefix(EntityEnemy __instance, DamageSource _damageSource, ref float __result)
        {
            if (__instance == null || _damageSource == null)
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }
}

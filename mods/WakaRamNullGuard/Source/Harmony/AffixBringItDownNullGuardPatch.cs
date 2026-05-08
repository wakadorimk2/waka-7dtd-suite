using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Hooked from EntityEnemy.DamageEntity Prefix. RAM does
    /// `_damageSource.AttackingItem` as the very first guard, which itself
    /// NREs when the damage source is null. Bail with __result=0f to leave
    /// vanilla damage scaling untouched.
    /// </summary>
    [HarmonyPatch]
    public static class AffixBringItDownNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.BringItDownReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.BringItDownReady) yield break;
            yield return RamBridge.BringItDownCheckMethod;
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

using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Hooked from EntityEnemy.DamageEntity Postfix. RAM checks AttackingItem
    /// via `_damageSource.AttackingItem == null` which NREs when _damageSource
    /// itself is null. Void return - just skip on null.
    /// </summary>
    [HarmonyPatch]
    public static class AffixGiantSlayerNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.GiantSlayerReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.GiantSlayerReady) yield break;
            yield return RamBridge.GiantSlayerCheckMethod;
        }

        public static bool Prefix(EntityEnemy __instance, DamageSource _damageSource)
        {
            if (__instance == null || _damageSource == null) return false;
            return true;
        }
    }
}

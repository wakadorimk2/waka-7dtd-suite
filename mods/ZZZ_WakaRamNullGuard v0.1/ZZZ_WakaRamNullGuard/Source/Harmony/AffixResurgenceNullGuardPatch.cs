using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Hooked from EntityPlayer.DamageEntity Postfix on every player hit. RAM
    /// dereferences `_damageSource.getEntityId()` without checking that the
    /// damage source itself is non-null first. Anything in vanilla that calls
    /// DamageEntity with a synthetic/null DamageSource (some mods, some
    /// environmental damage paths) would NRE every player hit.
    /// </summary>
    [HarmonyPatch]
    public static class AffixResurgenceNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.ResurgenceReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.ResurgenceReady) yield break;
            yield return RamBridge.ResurgenceCheckMethod;
        }

        public static bool Prefix(EntityPlayer __instance, DamageSource _damageSource)
        {
            if (__instance == null || _damageSource == null) return false;
            return true;
        }
    }
}

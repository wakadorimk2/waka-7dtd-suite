using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Hooked from ItemActionRanged.ConsumeAmmo Prefix - fires every shot.
    /// RAM does a 3-deep dereference
    /// `_actionData.invData.itemValue.ItemClass.HasAnyTags(...)` with no null
    /// checks at any level. Bail when any link is null.
    /// </summary>
    [HarmonyPatch]
    public static class AffixBulletRecoveryNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.BulletRecoveryReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.BulletRecoveryReady) yield break;
            yield return RamBridge.BulletRecoveryCheckMethod;
        }

        public static bool Prefix(ItemActionData _actionData)
        {
            if (_actionData == null) return false;
            var inv = _actionData.invData;
            if (inv == null) return false;
            var iv = inv.itemValue;
            if (iv == null || iv.ItemClass == null) return false;
            return true;
        }
    }
}

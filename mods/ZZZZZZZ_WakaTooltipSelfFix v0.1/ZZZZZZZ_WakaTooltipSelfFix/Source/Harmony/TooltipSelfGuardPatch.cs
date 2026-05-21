using System;
using System.Reflection;
using HarmonyLib;

namespace WakaTooltipSelfFix
{
    /// <summary>
    /// Vanilla bug: XUiM_ItemStack.GetCustomDisplayValueForItem only resets
    /// MinEventParams.CachedEventParam.ItemValue and Seed; it does NOT touch Self.
    /// When some other game event leaves Self == null (cache pollution), every
    /// food/drink triggered_effect ModifyCVar CanExecute check fails because
    /// MinEventActionTargetedBase.CanExecute requires _params.Self != null for
    /// targetType == self. Result: tooltip shows 0 for every CVar-based stat
    /// (foodAmountAdd, foodHealthAmount, $wakaProtein etc.) until something
    /// repopulates Self.
    ///
    /// Prefix restores Self to the local primary player before iteration.
    /// Only fills when null — never overwrites an active context.
    /// </summary>
    [HarmonyPatch]
    public static class TooltipSelfGuardPatch
    {
        static MethodInfo target;

        public static bool Prepare(MethodBase _)
        {
            if (target != null) return true;
            target = AccessTools.Method(typeof(XUiM_ItemStack), "GetCustomDisplayValueForItem");
            if (target == null)
            {
                Log.Warning("[WakaTooltipSelfFix] XUiM_ItemStack.GetCustomDisplayValueForItem not found, patch skipped");
                return false;
            }
            return true;
        }

        public static MethodBase TargetMethod() => target;

        public static void Prefix()
        {
            try
            {
                if (MinEventParams.CachedEventParam.Self != null) return;
                var world = GameManager.Instance?.World;
                if (world == null) return;
                var primary = world.GetPrimaryPlayer();
                if (primary != null)
                {
                    MinEventParams.CachedEventParam.Self = primary;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaTooltipSelfFix] Prefix guard failed: {e.Message}");
            }
        }
    }
}

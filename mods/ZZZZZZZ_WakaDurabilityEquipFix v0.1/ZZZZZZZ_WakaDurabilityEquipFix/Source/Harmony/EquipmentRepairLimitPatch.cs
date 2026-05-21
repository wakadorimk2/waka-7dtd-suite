using System;
using HarmonyLib;

namespace WakaDurabilityEquipFix
{
    /// <summary>
    /// Durability Overhaul v1.2 ships DisableAfterMaxRepairs as a Harmony Prefix
    /// on ItemActionEntryRepair.RefreshEnabled. That Prefix only inspects
    /// ItemController when it is a XUiC_ItemStack (inventory slot) and silently
    /// returns true for any other controller type, including XUiC_EquipmentStack
    /// (equipped slots). Because the repair button never gets Enabled = false on
    /// equipped items, OnDisabledActivate (MaxRepairTooltip) never fires; clicks
    /// fall through to vanilla repair via OnActivated, and AddRepairCount keeps
    /// incrementing the "repairs" metadata past MaxRepairs unbounded.
    ///
    /// This Postfix mirrors the same check for XUiC_EquipmentStack and forces
    /// Enabled = false once repairs >= MaxRepairs. Logic is duplicated rather
    /// than imported so we have no compile-time dependency on DurabilityOverhaul.dll.
    /// </summary>
    [HarmonyPatch(typeof(ItemActionEntryRepair), "RefreshEnabled")]
    public static class EquipmentRepairLimitPatch
    {
        public static void Postfix(ItemActionEntryRepair __instance)
        {
            try
            {
                var baseEntry = (BaseItemActionEntry)__instance;
                if (!(baseEntry.ItemController is XUiC_EquipmentStack equipStack)) return;
                var itemStack = equipStack.itemStack;
                if (itemStack == null) return;
                var itemValue = itemStack.itemValue;
                if (itemValue == null) return;
                if (!HasLimitedRepairs(itemValue)) return;
                int repairs = (int)itemValue.GetMetadata("repairs");
                int max = MaxRepairs(itemValue);
                if (repairs >= max)
                {
                    baseEntry.Enabled = false;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaDurabilityEquipFix] Postfix failed: {e.Message}");
            }
        }

        private static bool HasLimitedRepairs(ItemValue item)
        {
            return item.ItemClass != null
                && item.HasQuality
                && !item.ItemClass.Properties.GetBool("UnlimitedRepairs")
                && item.HasMetadata("repairs");
        }

        private static int MaxRepairs(ItemValue item)
        {
            if (item.HasMetadata("MaxRepairs"))
                return (int)item.GetMetadata("MaxRepairs");
            int num = item.Quality;
            if (item.ItemClass.Properties.Values.ContainsKey("MaxRepairs"))
            {
                string text = item.ItemClass.Properties.Values["MaxRepairs"];
                if (!text.Contains(","))
                {
                    int.TryParse(text, out num);
                }
                else
                {
                    string[] parts = text.Split(',');
                    if (parts.Length >= 2
                        && int.TryParse(parts[0], out int lo)
                        && int.TryParse(parts[1], out int hi))
                    {
                        num = lo + (hi - lo) / 5 * (item.Quality - 1);
                    }
                }
            }
            return num;
        }
    }
}

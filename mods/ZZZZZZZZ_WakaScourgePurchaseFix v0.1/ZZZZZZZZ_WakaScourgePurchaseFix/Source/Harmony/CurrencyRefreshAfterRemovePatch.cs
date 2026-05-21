using System;
using System.Reflection;
using HarmonyLib;

namespace WakaScourgePurchaseFix
{
    // Root cause:
    //   When the player's currency is stored in a nearby loot container (SCore's
    //   RemoteCrafting "ReadFromContainers" feature), buying from a trader runs
    //   through SCore's RemoteCraftingUtils.ConsumeItem. ConsumeItem decrements
    //   the container's items[y].count directly but does NOT fire any inventory
    //   change event on the player. The bag.DecItem call before the container
    //   loop fires events, but at that moment the container hasn't been
    //   decremented yet, so RefreshCurrency reads the still-old container count
    //   via SCore's GetItemCount Postfix and sees no change. After ConsumeItem
    //   finishes the container loop, no further event triggers RefreshCurrency,
    //   so CurrencyAmount stays stale until the next user action (sort, next
    //   purchase) happens to fire a fresh event.
    //
    //   Even calling RefreshCurrency() in a Postfix after ConsumeItem returns
    //   does not help because GetItemCount still reads stale values at that
    //   point — the container count modification appears to take effect after
    //   some additional internal sync.
    //
    // Fix:
    //   In the Postfix on RemoveItem(ItemStack), if CurrencyAmount did NOT
    //   change during the call AND the removed item type was the player's
    //   current currency, manually decrement CurrencyAmount by the requested
    //   count and fire OnCurrencyChanged so the BackpackWindow refreshes the
    //   {currencyamount} binding. This bypasses GetItemCount entirely.
    //
    //   When CurrencyAmount DID change (e.g. POI Scourge's RemoveItem Prefix
    //   already updated it, or vanilla bag.DecItem fired a working event),
    //   skip the manual update to avoid double-decrement.
    //
    //   The match key is the player inventory's `currencyItem` field (which
    //   POI Scourge swaps to scourgeToken at the Scourge Exchange Station).
    //   At a vanilla trader the field is casinoCoin and the removed item is
    //   casinoCoin → match. At the Scourge Station the field is scourgeToken
    //   but the removed item is casinoCoin → no match → skip (POI Scourge's
    //   own Prefix already handled the cvar update there). This intentionally
    //   targets only the regular trader / chest path that SCore's ConsumeItem
    //   leaves stranded.
    [HarmonyPatch(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.RemoveItem),
        new[] { typeof(ItemStack) })]
    public class WakaCurrencyRefreshAfterRemoveItem
    {
        private static FieldInfo _currencyItemField;
        private static FieldInfo _onCurrencyChangedField;

        [HarmonyPriority(0)]
        private static void Prefix(XUiM_PlayerInventory __instance, out int __state)
        {
            __state = __instance?.CurrencyAmount ?? -1;
        }

        [HarmonyPriority(0)]
        private static void Postfix(XUiM_PlayerInventory __instance, ItemStack _itemStack, int __state)
        {
            if (__instance == null || _itemStack == null || __state < 0) return;
            try
            {
                if (__instance.CurrencyAmount != __state) return;

                if (_itemStack.itemValue?.ItemClass == null) return;
                string removedName = _itemStack.itemValue.ItemClass.GetItemName();

                if (_currencyItemField == null)
                {
                    _currencyItemField = typeof(XUiM_PlayerInventory).GetField(
                        "currencyItem",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                var currencyItem = _currencyItemField?.GetValue(__instance) as ItemValue;
                string currencyName = currencyItem?.ItemClass?.GetItemName();

                bool isCurrencyMatch = currencyItem != null && _itemStack.itemValue.type == currencyItem.type;
                bool isScourgeBuy = removedName == "casinoCoin" && currencyName == "resourceScourgeToken";
                if (!isCurrencyMatch && !isScourgeBuy) return;

                int newAmount = Math.Max(0, __state - _itemStack.count);
                if (newAmount == __state) return;

                __instance.CurrencyAmount = newAmount;

                if (_onCurrencyChangedField == null)
                {
                    _onCurrencyChangedField = typeof(XUiM_PlayerInventory).GetField(
                        "OnCurrencyChanged",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                if (_onCurrencyChangedField?.GetValue(__instance) is MulticastDelegate del)
                {
                    del.DynamicInvoke();
                }

                Log.Out($"[WakaScourgePurchaseFix] Manual currency update {__state}->{newAmount} (removed {_itemStack.count} of currencyItem)");
            }
            catch (Exception ex)
            {
                Log.Error($"[WakaScourgePurchaseFix] Manual update failed: {ex.Message}");
            }
        }
    }
}

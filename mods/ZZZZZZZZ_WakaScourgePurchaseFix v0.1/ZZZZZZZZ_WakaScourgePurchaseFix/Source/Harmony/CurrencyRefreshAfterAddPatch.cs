using System;
using System.Reflection;
using HarmonyLib;

namespace WakaScourgePurchaseFix
{
    // Sell-side mirror of CurrencyRefreshAfterRemovePatch.
    //
    // Symptom: after selling at a regular trader, the {currencyamount}
    // binding lags by one beat. The next click (another sell, sort) refreshes
    // it.
    //
    // Root cause:
    //   Asylum Nearby Crafting installs a Postfix on
    //   XUiM_PlayerInventory.GetItemCount(ItemValue) that adds counts from
    //   nearby containers AND caches the result for 100ms
    //   (itemCountCache, Time.time - timestamp < 0.1f).
    //   When the trader window has just queried GetItemCount(currency),
    //   the cache is warm. AddItem then:
    //     bag.AddItem -> onBackpackItemsChanged -> RefreshCurrency
    //     -> GetItemCount(currency) -> cache hit, returns the stale
    //        pre-sell value, so RefreshCurrency sees no diff and skips
    //        firing OnCurrencyChanged.
    //   CurrencyAmount stays stale until the cache expires.
    //
    // Fix:
    //   In the Postfix on AddItem(ItemStack, bool), if CurrencyAmount did NOT
    //   change during the call AND the added item type was the player's
    //   current currencyItem, manually increment CurrencyAmount by the
    //   amount actually added and fire OnCurrencyChanged. This bypasses
    //   GetItemCount (and therefore the Asylum cache) entirely, mirroring
    //   the strategy used by the buy-side patch.
    //
    //   When CurrencyAmount DID change (the normal event path got through),
    //   skip to avoid double-increment.
    //
    // Notes on coverage:
    //   - vanilla sell flow ends in playerInventory.AddItem(itemStack2, false)
    //     with itemStack2 = currencyItem * sellPrice, so the 2-arg overload
    //     is the canonical entry.
    //   - the 1-arg AddItem(ItemStack) just delegates to the 2-arg, so
    //     patching the 2-arg also covers any caller of the 1-arg.
    //   - vanilla AddItem mutates _itemStack.count (decrements by what was
    //     placed), so the "amount actually added" is captured as
    //     originalCount - _itemStack.count after the call.
    //   - itemValue could in theory be reset on the passed-in stack after
    //     it goes empty, so the original type is captured in the Prefix.
    [HarmonyPatch(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.AddItem),
        new[] { typeof(ItemStack), typeof(bool) })]
    public class WakaCurrencyRefreshAfterAddItem
    {
        private static FieldInfo _currencyItemField;
        private static FieldInfo _onCurrencyChangedField;

        public struct AddState
        {
            public int CurrencyAmount;
            public int OriginalCount;
            public int OriginalType;
            public bool Valid;
        }

        [HarmonyPriority(0)]
        private static void Prefix(XUiM_PlayerInventory __instance, ItemStack _itemStack, out AddState __state)
        {
            __state = default;
            if (__instance == null || _itemStack == null || _itemStack.itemValue == null) return;
            __state.CurrencyAmount = __instance.CurrencyAmount;
            __state.OriginalCount = _itemStack.count;
            __state.OriginalType = _itemStack.itemValue.type;
            __state.Valid = true;
        }

        [HarmonyPriority(0)]
        private static void Postfix(XUiM_PlayerInventory __instance, ItemStack _itemStack, AddState __state)
        {
            if (!__state.Valid || __instance == null) return;
            try
            {
                if (__state.OriginalCount <= 0) return;

                int remaining = _itemStack?.count ?? 0;
                int added = __state.OriginalCount - remaining;
                if (added <= 0) return;

                if (__instance.CurrencyAmount != __state.CurrencyAmount) return;

                if (_currencyItemField == null)
                {
                    _currencyItemField = typeof(XUiM_PlayerInventory).GetField(
                        "currencyItem",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
                var currencyItem = _currencyItemField?.GetValue(__instance) as ItemValue;
                if (currencyItem == null) return;
                if (__state.OriginalType != currencyItem.type) return;

                int newAmount = __state.CurrencyAmount + added;
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

                Log.Out($"[WakaScourgePurchaseFix] Manual currency update (sell) {__state.CurrencyAmount}->{newAmount} (added {added} of currencyItem)");
            }
            catch (Exception ex)
            {
                Log.Error($"[WakaScourgePurchaseFix] Sell-side manual update failed: {ex.Message}");
            }
        }
    }
}

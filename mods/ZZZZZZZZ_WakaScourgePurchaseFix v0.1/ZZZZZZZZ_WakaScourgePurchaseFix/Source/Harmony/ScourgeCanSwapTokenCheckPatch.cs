using HarmonyLib;

namespace WakaScourgePurchaseFix
{
    // POI Scourge's ScourgeCanSwapPatch (Priority 800) only checks inventory space when
    // the player tries to spend casinoCoin at the Scourge Exchange Station. It ignores
    // the cvar_scourge_tokens balance, so a stale "Buy" button enabled state plus the
    // Math.Max(0f, ...) clamp inside ScourgeRemoveItemPatch lets rapid clicks drain
    // items for free.
    //
    // v0.1 used a Prefix at Priority 900 to short-circuit with __result=false. That
    // turned out to be ineffective: Harmony 2 does NOT stop subsequent Prefixes when
    // one returns false (it only sets runOriginal=false), so POI Scourge's own Prefix
    // still runs afterwards and unconditionally overwrites __result with the space
    // check. The buy goes through whenever there is room in the bag.
    //
    // v0.4 fix: enforce the balance check as a Postfix instead. The Postfix runs after
    // every Prefix has executed, so it gets the final say on __result. When __result
    // is already false (POI Scourge said "no space"), bail out — we don't need to
    // double-block. When it's true, re-verify against cvar_scourge_tokens and flip to
    // false if the player can't actually pay.
    [HarmonyPatch(typeof(XUiM_PlayerInventory), nameof(XUiM_PlayerInventory.CanSwapItems))]
    public class ScourgeCanSwapTokenCheckPatch
    {
        [HarmonyPriority(0)]
        private static void Postfix(XUiM_PlayerInventory __instance, ItemStack _removedStack, ref bool __result)
        {
            if (!__result) return;
            if (__instance == null) return;
            if (_removedStack?.itemValue?.ItemClass == null) return;

            string itemName = _removedStack.itemValue.ItemClass.GetItemName();
            if (itemName != "casinoCoin") return;

            if (!IsScourgeStationOpen(__instance.xui)) return;

            var localPlayer = __instance.localPlayer;
            if (localPlayer == null) return;

            float cvarTokens = localPlayer.Buffs.GetCustomVar("cvar_scourge_tokens");
            if ((int)cvarTokens < _removedStack.count)
            {
                __result = false;
                Log.Out($"[WakaScourgePurchaseFix] CanSwap blocked at scourge station: tokens={cvarTokens}, need={_removedStack.count}");
            }
        }

        private static bool IsScourgeStationOpen(XUi xui)
        {
            var traderInfo = xui?.Trader?.Trader?.TraderInfo;
            if (traderInfo == null) return false;

            if (traderInfo.Id == 99) return true;

            var tile = xui.Trader.TraderTileEntity;
            if (tile == null) return false;

            var world = GameManager.Instance?.World;
            if (world == null) return false;

            var block = world.GetBlock(tile.ToWorldPos()).Block;
            return block != null && block.GetBlockName() == "scourgeExchangeStation";
        }
    }
}

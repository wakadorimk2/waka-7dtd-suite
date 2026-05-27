using HarmonyLib;

namespace WakaToolbelt16Sync
{
    [HarmonyPatch(typeof(Inventory), "get_PUBLIC_SLOTS_PLAYMODE")]
    internal static class InventoryPublicSlotsPlaymodePatch
    {
        private const int ToolbeltSlots = 16;

        private static void Postfix(ref int __result)
        {
            __result = ToolbeltSlots;
        }
    }
}

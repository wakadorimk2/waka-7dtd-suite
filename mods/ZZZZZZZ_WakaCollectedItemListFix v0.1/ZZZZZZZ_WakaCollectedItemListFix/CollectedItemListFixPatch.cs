using HarmonyLib;
using UnityEngine;

namespace WakaCollectedItemListFix
{
    // Vanilla XUiC_CollectedItemList.removeLastEntry hands the toast
    // GameObject to a TemporaryObject MonoBehaviour, which is supposed to
    // call Object.Destroy after 2s via a Start()-launched coroutine. Unity
    // does not invoke Start() on inactive GameObjects, so when the HUD goes
    // inactive between the entry being removed from the items list and the
    // coroutine firing (e.g. ESC pressed mid-pickup-burst from a TakeAll on
    // a 15+ item container), the GameObject is orphaned: gone from the list
    // but still rendered, overlapping any subsequent toasts.
    //
    // Calling Object.Destroy directly bypasses the active-state requirement
    // (the engine handles destruction itself). Vanilla's tween/temporary-obj
    // setup runs after this Prefix on a marked-for-destruction object - the
    // (bool)item check there returns false thanks to Unity's overloaded ==,
    // so the tween block is skipped harmlessly. items.RemoveAt and
    // updateEntries still run as normal.
    [HarmonyPatch(typeof(XUiC_CollectedItemList), "removeLastEntry")]
    public static class CollectedItemListFixPatch
    {
        public static void Prefix(XUiC_CollectedItemList __instance, int index)
        {
            if (__instance == null) return;
            var items = __instance.items;
            if (items == null || index < 0 || index >= items.Count) return;
            var item = items[index].Item;
            if (item != null)
            {
                Object.Destroy(item);
            }
        }
    }
}

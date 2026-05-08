using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Skips RAM's loot affix application when player is null. RAM hooks
    /// LootContainer.SpawnItem.Postfix and forwards the player parameter
    /// blindly, but vanilla calls SpawnItem with player=null for non-player
    /// loot generation (POI prefab init, sleeper container init, etc.). The
    /// downstream RandomizeTierWithOdds then logs "Player progression not
    /// found" once per spawned item, spamming the log without doing anything
    /// useful (no player = no Magic Find, no challenge bonus, nothing to
    /// apply meaningfully).
    /// </summary>
    [HarmonyPatch]
    public static class ApplyAffixToLootCheckPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.LootCheckReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.LootCheckReady) yield break;
            yield return RamBridge.ApplyAffixToLootCheckMethod;
        }

        public static bool Prefix(EntityPlayer player)
        {
            if (player == null)
            {
                return false;
            }
            return true;
        }
    }
}

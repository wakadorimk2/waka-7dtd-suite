using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Prefix that adds null checks RAM forgot. RAM's original code does:
    ///   foreach (g in ((EntityAlive)player).challengeJournal.CompleteChallengeGroupsForMinEvents)
    /// with no null guards. Vanilla itself treats challengeJournal as nullable
    /// (see EntityAlive.OnFireMinEvent), so a null player or unset journal
    /// throws NRE during loot affix application, killing affix placement for
    /// the entire stack.
    ///
    /// When the precondition fails we set __result=false and skip the original
    /// (return false from prefix). The "false" return matches RAM's intent:
    /// challenge group is not yet completed so no bonus tier is granted.
    /// </summary>
    [HarmonyPatch]
    public static class ChallengeGroupNullGuardPatch
    {
        public static bool Prepare(MethodBase _) => RamBridge.ChallengeReady;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!RamBridge.ChallengeReady) yield break;
            yield return RamBridge.ChallengeGroupIsCompletedMethod;
        }

        public static bool Prefix(EntityPlayer player, string groupName, ref bool __result)
        {
            try
            {
                if (player == null)
                {
                    __result = false;
                    return false;
                }
                var ea = player as EntityAlive;
                if (ea == null || ea.challengeJournal == null)
                {
                    __result = false;
                    return false;
                }
                if (ea.challengeJournal.CompleteChallengeGroupsForMinEvents == null)
                {
                    __result = false;
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaRamNullGuard] Prefix guard failed: {e.Message}");
                __result = false;
                return false;
            }
        }
    }
}

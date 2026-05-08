using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaInfestedQuestTier
{
    /// <summary>
    /// Wraps every instance method on POIScourge.ItemActionStartScourgeQuest
    /// that takes an ItemActionData parameter. In the prefix we read the
    /// player's current POI difficulty tier and, if a tier-aware variant of
    /// the configured QuestID exists (quest_scourge_infestation_tN), swap the
    /// instance's QuestID storage to it for the duration of the call. The
    /// postfix restores the original value.
    ///
    /// The QuestID is read/written via ScourgeBridge which abstracts whether
    /// the storage is a field, a property, or an entry in a string-string
    /// dictionary on the instance.
    /// </summary>
    public static class ScourgeQuestPatch
    {
        public class State
        {
            public string OriginalQuestId;
            public bool Restore;
        }

        public static bool Prepare(MethodBase _) => ScourgeBridge.Ready;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!ScourgeBridge.Ready) yield break;
            var t = ScourgeBridge.ItemActionType;
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var m in t.GetMethods(bf))
            {
                if (m.IsAbstract) continue;
                var ps = m.GetParameters();
                foreach (var p in ps)
                {
                    if (p.ParameterType != null && p.ParameterType.Name == "ItemActionData")
                    {
                        yield return m;
                        break;
                    }
                }
            }
        }

        public static void Prefix(object __instance, object[] __args, out State __state)
        {
            __state = null;
            try
            {
                if (!ScourgeBridge.Ready || __instance == null) return;

                var orig = ScourgeBridge.ReadQuestId(__instance);
                if (string.IsNullOrEmpty(orig)) return;

                var player = ResolvePlayer(__args);
                if (player == null) return;

                int tier = ScourgeBridge.GetPlayerPOITier(player);
                if (tier < 1) return;

                string newId = $"{orig}_t{tier}";
                if (!QuestExists(newId))
                {
                    Log.Out($"[WakaInfestedQuestTier] Tier {tier} variant '{newId}' not registered, keeping '{orig}'");
                    return;
                }

                if (!ScourgeBridge.WriteQuestId(__instance, newId)) return;
                __state = new State { OriginalQuestId = orig, Restore = true };
                Log.Out($"[WakaInfestedQuestTier] Redirected '{orig}' -> '{newId}' (POI tier {tier})");
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] Prefix swap failed: {e.Message}");
            }
        }

        public static void Postfix(object __instance, State __state)
        {
            if (__state == null || !__state.Restore) return;
            try
            {
                ScourgeBridge.WriteQuestId(__instance, __state.OriginalQuestId);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaInfestedQuestTier] Postfix restore failed: {e.Message}");
            }
        }

        static EntityPlayerLocal ResolvePlayer(object[] args)
        {
            if (args != null)
            {
                foreach (var a in args)
                {
                    if (a == null) continue;
                    var iad = a as ItemActionData;
                    if (iad?.invData?.holdingEntity is EntityPlayerLocal p1) return p1;
                }
            }
            return GameManager.Instance?.World?.GetPrimaryPlayer() as EntityPlayerLocal;
        }

        static bool QuestExists(string id)
        {
            try
            {
                if (QuestClass.s_Quests != null && QuestClass.s_Quests.ContainsKey(id)) return true;
            }
            catch { }
            return false;
        }
    }

    [HarmonyPatch]
    public static class ScourgeQuestPatchHarness
    {
        public static bool Prepare(MethodBase original) => ScourgeQuestPatch.Prepare(original);
        public static IEnumerable<MethodBase> TargetMethods() => ScourgeQuestPatch.TargetMethods();
        public static void Prefix(object __instance, object[] __args, out ScourgeQuestPatch.State __state)
            => ScourgeQuestPatch.Prefix(__instance, __args, out __state);
        public static void Postfix(object __instance, ScourgeQuestPatch.State __state)
            => ScourgeQuestPatch.Postfix(__instance, __state);
    }
}

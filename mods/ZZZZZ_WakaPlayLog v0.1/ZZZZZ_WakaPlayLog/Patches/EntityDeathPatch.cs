using System;
using HarmonyLib;

namespace WakaPlayLog.Patches
{
    /// <summary>
    /// Postfix on EntityAlive.OnEntityDeath. Fires player_death event when
    /// the dying entity is the local player. ModEvents.EntityKilled does
    /// not always fire reliably for player death and lacks damage source
    /// context, so we hook the death path directly.
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive), "OnEntityDeath")]
    public static class EntityAlive_OnEntityDeath_Patch
    {
        public static void Postfix(EntityAlive __instance)
        {
            try
            {
                if (__instance == null) return;
                if (!(__instance is EntityPlayerLocal)) return;
                EntityAlive killer = null;
                try { killer = __instance.entityThatKilledMe as EntityAlive; } catch { }
                CombatEvents.HandlePlayerDeath(__instance, killer, 0, null);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] EntityAlive_OnEntityDeath_Patch failed: {e.Message}");
            }
        }
    }
}

using HarmonyLib;

namespace WakaSoloServerPause
{
    [HarmonyPatch(typeof(EntityBuffs), nameof(EntityBuffs.Tick))]
    public static class EntityBuffs_Tick_PausePatch
    {
        public static bool Prefix(EntityBuffs __instance)
        {
            EntityAlive entity = __instance?.parent;
            return !PauseManager.ShouldPauseBuffTick(entity) &&
                   !ClientEscMenuPauseBridge.IsLocalPausedPlayer(entity);
        }
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.updateTimeOfDay))]
    public static class GameManager_UpdateTimeOfDay_PausePatch
    {
        public static bool Prefix()
        {
            return !PauseManager.IsActive && !ClientEscMenuPauseBridge.IsLocalPauseActive;
        }
    }

    [HarmonyPatch(typeof(World), nameof(World.SetTime))]
    public static class World_SetTime_PausePatch
    {
        public static void Prefix(ref ulong _time)
        {
            if (PauseManager.IsActive)
            {
                _time = PauseManager.FrozenWorldTime;
            }
            else if (ClientEscMenuPauseBridge.IsLocalPauseActive)
            {
                _time = ClientEscMenuPauseBridge.LocalFrozenWorldTime;
            }
        }
    }

    [HarmonyPatch(typeof(EntityStats), nameof(EntityStats.Tick))]
    public static class EntityStats_Tick_PausePatch
    {
        public static bool Prefix(EntityAlive ___m_entity)
        {
            return !PauseManager.ShouldPauseEntitySystem(___m_entity) &&
                   !ClientEscMenuPauseBridge.IsLocalPausedPlayer(___m_entity);
        }
    }

    [HarmonyPatch(typeof(PlayerEntityStats), nameof(PlayerEntityStats.TickWait))]
    public static class PlayerEntityStats_TickWait_PausePatch
    {
        public static bool Prefix(EntityAlive ___m_entity)
        {
            return !PauseManager.IsEntityPaused(___m_entity) &&
                   !ClientEscMenuPauseBridge.IsLocalPausedPlayer(___m_entity);
        }
    }

    [HarmonyPatch(typeof(EntityAlive), nameof(EntityAlive.DamageEntity))]
    public static class EntityAlive_DamageEntity_PausePatch
    {
        public static bool Prefix(EntityAlive __instance, ref int __result)
        {
            if (!PauseManager.ShouldPauseEntitySystem(__instance))
            {
                return true;
            }

            __result = -1;
            return false;
        }
    }
}

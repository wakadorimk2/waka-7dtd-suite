using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace WakaPlayLog
{
    public class WakaPlayLogInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaPlayLog] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakaplaylog");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
                ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);
                ModEvents.EntityKilled.RegisterHandler(OnEntityKilled);
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);

                Log.Out("[WakaPlayLog] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaPlayLog] InitMod failed: {e}");
            }
        }

        static void OnGameStartDone(ref ModEvents.SGameStartDoneData _data)
        {
            try
            {
                GameState.Reset();
                LogWriter.StartSession();
                LogWriter.Write("meta", "session_start", "notable", new Dictionary<string, object>
                {
                    { "started_at", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                    { "game_day", GameTime.CurrentDay() },
                });
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] OnGameStartDone failed: {e.Message}");
            }
        }

        static void OnGameShutdown(ref ModEvents.SGameShutdownData _data)
        {
            try
            {
                MovementEvents.HandleSessionEnd();
                LogWriter.Write("meta", "session_end", "notable", new Dictionary<string, object>
                {
                    { "ended_at", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") },
                    { "deaths_this_session", GameState.Deaths },
                });
                LogWriter.EndSession();
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] OnGameShutdown failed: {e.Message}");
            }
        }

        static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            try
            {
                if (!LogWriter.IsActive) return;
                if (!GameState.ShouldPollNow()) return;
                PollingTick.Run();
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] OnGameUpdate failed: {e.Message}");
            }
        }

        static void OnEntityKilled(ref ModEvents.SEntityKilledData _data)
        {
            try
            {
                CombatEvents.HandleEntityKilled(_data.KilledEntitiy, _data.KillingEntity);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] OnEntityKilled failed: {e.Message}");
            }
        }

        static void OnPlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData _data)
        {
            try
            {
                if (!LogWriter.IsActive) return;
                LogWriter.Write("meta", "player_spawned", "normal", new Dictionary<string, object>());
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] OnPlayerSpawnedInWorld failed: {e.Message}");
            }
        }
    }
}

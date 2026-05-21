using System;
using System.Reflection;
using HarmonyLib;

namespace WakaSoloServerPause
{
    public class WakaSoloServerPauseInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaSoloServerPause] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakasoloserverpause");
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                ModEvents.ChatMessage.RegisterHandler(PauseChatHandler.OnChatMessage);
                ModEvents.GameUpdate.RegisterHandler(PauseManager.OnGameUpdate);
                ModEvents.GameShutdown.RegisterHandler(PauseManager.OnGameShutdown);

                Log.Out("[WakaSoloServerPause] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaSoloServerPause] InitMod failed: {e}");
            }
        }
    }
}

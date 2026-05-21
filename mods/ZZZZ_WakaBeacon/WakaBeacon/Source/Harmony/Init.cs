using System;
using System.Reflection;
using HarmonyLib;

namespace WakaBeacon
{
    public class WakaBeaconInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaBeacon] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakabeacon");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                ModEvents.GameUpdate.RegisterHandler(WakaBeaconManager.OnGameUpdate);
                ModEvents.GameShutdown.RegisterHandler(WakaBeaconManager.OnGameShutdown);
                Log.Out("[WakaBeacon] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaBeacon] InitMod failed: {e}");
            }
        }
    }
}

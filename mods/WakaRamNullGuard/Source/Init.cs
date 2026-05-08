using System;
using System.Reflection;
using HarmonyLib;

namespace WakaRamNullGuard
{
    public class WakaRamNullGuardInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaRamNullGuard] InitMod start");
            try
            {
                RamBridge.Initialize();
                if (!RamBridge.AnyReady)
                {
                    Log.Out("[WakaRamNullGuard] RAM mod not detected, skipping patches");
                    return;
                }
                var harmony = new Harmony("wakadori.wakaramnullguard");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaRamNullGuard] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaRamNullGuard] InitMod failed: {e}");
            }
        }
    }
}

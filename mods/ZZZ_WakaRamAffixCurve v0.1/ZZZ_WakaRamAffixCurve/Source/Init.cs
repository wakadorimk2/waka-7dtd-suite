using System;
using System.Reflection;
using HarmonyLib;

namespace WakaRamAffixCurve
{
    public class WakaRamAffixCurveInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaRamAffixCurve] InitMod start");
            try
            {
                RamCurveBridge.Initialize();
                if (!RamCurveBridge.Ready)
                {
                    Log.Out("[WakaRamAffixCurve] RAM mod not detected, skipping patches");
                    return;
                }
                var harmony = new Harmony("wakadori.wakaramaffixcurve");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaRamAffixCurve] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaRamAffixCurve] InitMod failed: {e}");
            }
        }
    }
}

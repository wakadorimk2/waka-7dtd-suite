using System;
using System.Reflection;
using HarmonyLib;

namespace WakaTooltipSelfFix
{
    public class WakaTooltipSelfFixInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaTooltipSelfFix] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakatooltipselffix");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaTooltipSelfFix] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaTooltipSelfFix] InitMod failed: {e}");
            }
        }
    }
}

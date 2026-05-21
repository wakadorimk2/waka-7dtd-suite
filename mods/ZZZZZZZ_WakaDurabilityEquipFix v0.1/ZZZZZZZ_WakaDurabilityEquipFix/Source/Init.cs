using System;
using System.Reflection;
using HarmonyLib;

namespace WakaDurabilityEquipFix
{
    public class WakaDurabilityEquipFixInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaDurabilityEquipFix] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakadurabilityequipfix");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaDurabilityEquipFix] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaDurabilityEquipFix] InitMod failed: {e}");
            }
        }
    }
}

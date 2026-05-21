using System;
using System.Reflection;
using HarmonyLib;

namespace WakaScourgePurchaseFix
{
    public class WakaScourgePurchaseFixInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaScourgePurchaseFix] InitMod start");
            try
            {
                var harmony = new Harmony("wakadori.wakascourgepurchasefix");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaScourgePurchaseFix] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaScourgePurchaseFix] InitMod failed: {e}");
            }
        }
    }
}

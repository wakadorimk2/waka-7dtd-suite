using System;
using System.Reflection;

namespace WakaLinkCrafting
{
    public class WakaLinkCraftingInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaLinkCrafting] InitMod start");
            try
            {
                var harmony = new HarmonyLib.Harmony("wakadori.wakalinkcrafting");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaLinkCrafting] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaLinkCrafting] InitMod failed: {e}");
            }
        }
    }
}

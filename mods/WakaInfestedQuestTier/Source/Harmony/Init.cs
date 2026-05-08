using System;
using System.Reflection;
using HarmonyLib;

namespace WakaInfestedQuestTier
{
    public class WakaInfestedQuestTierInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaInfestedQuestTier] InitMod start");
            try
            {
                ScourgeBridge.Initialize();
                QuestCompletePatch.Initialize();
                var harmony = new Harmony("wakadori.wakainfestedquesttier");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaInfestedQuestTier] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaInfestedQuestTier] InitMod failed: {e}");
            }
        }
    }
}

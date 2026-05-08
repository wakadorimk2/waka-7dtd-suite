using System;
using System.Reflection;
using HarmonyLib;

namespace WakaQuestProgression
{
    public class WakaQuestProgressionInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaQuestProgression] InitMod start");
            try
            {
                ScourgeBridge.Initialize();
                QuestCompletePatch.Initialize();
                var harmony = new Harmony("wakadori.wakaquestprogression");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaQuestProgression] InitMod done");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaQuestProgression] InitMod failed: {e}");
            }
        }
    }
}

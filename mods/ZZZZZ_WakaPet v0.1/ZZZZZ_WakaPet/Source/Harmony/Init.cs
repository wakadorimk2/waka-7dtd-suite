using System;
using System.Reflection;
using HarmonyLib;

namespace WakaPet
{
    public class WakaPetInit : IModApi
    {
        static bool inited;

        public void InitMod(Mod _modInstance)
        {
            if (inited) return;
            inited = true;

            Log.Out("[WakaPet] InitMod start");

            // Voice clip プリロード（patch 前に走らせて、最初の trigger 発火に間に合わせる）
            try
            {
                WakaPetVoice.Init(_modInstance.Path);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPet] WakaPetVoice.Init failed: {e.Message}");
            }

            var harmony = new HarmonyLib.Harmony("wakadori.wakapet");

            // patch クラスを 1 つずつ個別 try-catch で attach（1 つ失敗しても他は生かす）
            int ok = 0, ng = 0;
            foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (t.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                try
                {
                    new PatchClassProcessor(harmony, t).Patch();
                    Log.Out($"[WakaPet] patched: {t.Name}");
                    ok++;
                }
                catch (Exception e)
                {
                    Log.Warning($"[WakaPet] patch FAILED for {t.Name}: {e.Message}");
                    ng++;
                }
            }

            Log.Out($"[WakaPet] InitMod done: {ok} patched, {ng} failed");
        }
    }
}

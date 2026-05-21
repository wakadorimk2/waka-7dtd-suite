using HarmonyLib;
using System.Reflection;

namespace WakaCatuiStormTimerFix
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.CatuiStormTimerFix");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaCatuiStormTimerFix] Harmony patches applied.");
        }
    }
}

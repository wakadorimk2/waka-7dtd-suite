using HarmonyLib;
using System.Reflection;

namespace WakaToolbelt16Sync
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.Toolbelt16Sync");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaToolbelt16Sync] Harmony patches applied.");
        }
    }
}

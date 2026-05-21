using HarmonyLib;
using System.Reflection;

namespace WakaCollectedItemListFix
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.CollectedItemListFix");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaCollectedItemListFix] Harmony patches applied.");
        }
    }
}

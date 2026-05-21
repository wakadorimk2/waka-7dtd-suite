using HarmonyLib;
using System.Reflection;

namespace WakaNhxBagAutoOpen
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.NhxBagAutoOpen");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaNhxBagAutoOpen] Harmony patches applied.");
        }
    }
}

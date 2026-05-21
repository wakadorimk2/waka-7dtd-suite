using HarmonyLib;
using System.Reflection;

namespace WakaNearbyCraftingBridge
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.NearbyCraftingBridge");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaNearbyCraftingBridge] Harmony patches applied.");
        }
    }
}

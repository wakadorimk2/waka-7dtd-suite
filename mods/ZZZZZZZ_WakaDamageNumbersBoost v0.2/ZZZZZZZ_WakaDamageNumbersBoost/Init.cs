using HarmonyLib;
using System.Reflection;

namespace WakaDamageNumbersBoost
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.DamageNumbersBoost");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaDamageNumbersBoost] Harmony patches applied.");
        }
    }
}

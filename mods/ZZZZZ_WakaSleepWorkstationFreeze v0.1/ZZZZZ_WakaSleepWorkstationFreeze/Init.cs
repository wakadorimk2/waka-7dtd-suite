using HarmonyLib;
using System.Reflection;

namespace WakaSleepWorkstationFreeze
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.SleepWorkstationFreeze");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaSleepWorkstationFreeze] Harmony patches applied.");
        }
    }
}

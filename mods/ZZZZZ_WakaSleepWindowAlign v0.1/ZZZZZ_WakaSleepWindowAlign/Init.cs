using HarmonyLib;
using System.Reflection;

namespace WakaSleepWindowAlign
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.SleepWindowAlign");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaSleepWindowAlign] Harmony patches applied.");
        }
    }
}

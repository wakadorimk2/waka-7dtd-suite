using HarmonyLib;
using System.Reflection;

namespace WakaChallengeBadge
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.ChallengeBadge");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaChallengeBadge] Harmony patches applied.");
        }
    }
}

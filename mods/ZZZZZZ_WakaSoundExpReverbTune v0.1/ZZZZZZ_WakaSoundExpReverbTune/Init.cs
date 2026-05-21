using HarmonyLib;
using System.Reflection;

namespace WakaSoundExpReverbTune
{
    public class Init : IModApi
    {
        public void InitMod(Mod _mod)
        {
            var harmony = new Harmony("Waka.SoundExpReverbTune");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Out("[WakaSoundExpReverbTune] Harmony patches applied.");
        }
    }
}

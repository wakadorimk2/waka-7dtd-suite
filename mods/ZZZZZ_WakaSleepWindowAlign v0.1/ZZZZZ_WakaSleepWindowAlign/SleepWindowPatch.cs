using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace WakaSleepWindowAlign
{
    // Sleep Overhaul gates both the X-key path (BedrollPatches.HandleSleepPrefix)
    // and the dialog-confirm path (BedrollXuiGuards.ConfirmPrefix) through a single
    // helper: BedrollUtil.IsWithinSleepWindow(World w). Vanilla returns true for
    // worldHour >= 20 || worldHour < 2, i.e. 20:00-01:59. Singularity / Advanced
    // Sky Manager defines night as Dusk(20) to Dawn(5), so we extend the window
    // end from 02:00 to 05:00 to let the player sleep through the whole night.
    [HarmonyPatch]
    public static class BedrollUtil_IsWithinSleepWindow_Patch
    {
        private const int DuskHour = 20;
        private const int DawnHour = 5;

        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a =>
                    {
                        var n = a.GetName().Name;
                        return n == "Bedroll Sleeping" || n == "BedrollSleeping";
                    });
                if (asm == null)
                {
                    Log.Warning("[WakaSleepWindowAlign] Sleep Overhaul assembly not found, patch will be inert.");
                    return null;
                }
                var t = asm.GetType("BedrollUtil");
                if (t == null)
                {
                    Log.Warning("[WakaSleepWindowAlign] BedrollUtil type not found in Sleep Overhaul assembly.");
                    return null;
                }
                var m = t.GetMethod("IsWithinSleepWindow", BindingFlags.Public | BindingFlags.Static);
                if (m == null)
                {
                    Log.Warning("[WakaSleepWindowAlign] IsWithinSleepWindow method not found on BedrollUtil.");
                }
                return m;
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSleepWindowAlign] TargetMethod lookup failed: " + e);
                return null;
            }
        }

        public static bool Prefix(World __0, ref bool __result)
        {
            if (__0 == null)
            {
                return true;
            }
            int worldHour = (int)(__0.worldTime % 24000UL / 1000UL);
            __result = worldHour >= DuskHour || worldHour < DawnHour;
            return false;
        }
    }
}

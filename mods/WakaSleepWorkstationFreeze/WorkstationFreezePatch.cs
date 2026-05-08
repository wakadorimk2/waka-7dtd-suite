using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace WakaSleepWorkstationFreeze
{
    // Sleep Overhaul ships a static class BedrollSleepTimeSkip with
    //     public static void Advance(World world, float seconds)
    // which iterates every TileEntityWorkstation / TileEntityForge in the
    // loaded chunks and runs HandleFuel / HandleRecipeQueue / HandleMaterialInput
    // for `seconds` worth of in-game time. That is what burns fuel during sleep.
    //
    // Prefix returns false to suppress the entire fast-forward pass.
    // World clock still advances (Sleep Overhaul drives it via World.SetTime
    // elsewhere); only the per-tile-entity fuel/queue replay is skipped.
    //
    // Side effect: in-progress crafting also does not advance during sleep.
    // That trade-off is acceptable for now; revisit if needed by patching
    // the inner ProcessTE method instead and skipping only HandleFuel.
    [HarmonyPatch]
    public static class BedrollSleepTimeSkip_Advance_Patch
    {
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
                    Log.Warning("[WakaSleepWorkstationFreeze] Sleep Overhaul assembly not found, patch will be inert.");
                    return null;
                }
                var t = asm.GetType("BedrollSleepTimeSkip");
                if (t == null)
                {
                    Log.Warning("[WakaSleepWorkstationFreeze] BedrollSleepTimeSkip type not found in Sleep Overhaul assembly.");
                    return null;
                }
                var m = t.GetMethod("Advance", BindingFlags.Public | BindingFlags.Static);
                if (m == null)
                {
                    Log.Warning("[WakaSleepWorkstationFreeze] Advance method not found on BedrollSleepTimeSkip.");
                }
                return m;
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSleepWorkstationFreeze] TargetMethod lookup failed: " + e);
                return null;
            }
        }

        public static bool Prefix()
        {
            return false;
        }
    }
}

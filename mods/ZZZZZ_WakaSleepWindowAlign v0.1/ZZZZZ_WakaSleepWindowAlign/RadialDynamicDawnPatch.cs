using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace WakaSleepWindowAlign
{
    // *** DORMANT PATCH (kept as insurance) ***
    // Sleep Overhaul defines BedrollPatches.HandleSleepPrefix which intercepts
    // Block.OnBlockActivated and, if the command name == "sleep8_action", runs
    //     SleepRunner.Run(__5, 8);
    //     return false;
    // We rewrite that literal 8 to DawnHelper.GetHoursUntilDawn().
    //
    // HOWEVER: in the shipped Sleep Overhaul v1.3.1 the "sleep8_action" command
    // is never actually emitted -- vanilla BlockSleepingBag does not declare it,
    // and Sleep Overhaul's RadialPostfix (which would inject it) is defined in
    // BedrollPatches but NOT registered in BedrollSleepMod.InitMod. So today
    // HandleSleepPrefix's sleep branch never runs and this transpile patches
    // dead code. The only live sleep path is the X-key dialog (XUiC_TFDSleepDialog).
    //
    // We keep the patch in place so that if a future Sleep Overhaul update wires
    // the radial up, the radial will already behave the way we want it (sleep
    // till dawn instead of fixed 8h). Until then, this is a harmless no-op.
    [HarmonyPatch]
    public static class HandleSleepPrefix_DynamicDawn_Transpiler
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
                    Log.Warning("[WakaSleepWindowAlign] Sleep Overhaul assembly not found for radial transpile.");
                    return null;
                }
                var t = asm.GetType("BedrollPatches");
                if (t == null)
                {
                    Log.Warning("[WakaSleepWindowAlign] BedrollPatches type not found.");
                    return null;
                }
                var m = t.GetMethod("HandleSleepPrefix", BindingFlags.Public | BindingFlags.Static);
                if (m == null)
                {
                    Log.Warning("[WakaSleepWindowAlign] HandleSleepPrefix method not found.");
                }
                return m;
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSleepWindowAlign] Radial TargetMethod lookup failed: " + e);
                return null;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);

            // Locate the SleepRunner.Run(EntityPlayerLocal, int) method on the
            // Sleep Overhaul assembly (it is internal, so we cannot reference it
            // directly from this assembly).
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                {
                    var n = a.GetName().Name;
                    return n == "Bedroll Sleeping" || n == "BedrollSleeping";
                });
            var sleepRunnerType = asm?.GetType("SleepRunner");
            var runMethod = sleepRunnerType?.GetMethod("Run", BindingFlags.Public | BindingFlags.Static);
            if (runMethod == null)
            {
                Log.Warning("[WakaSleepWindowAlign] SleepRunner.Run not found, radial transpile inert.");
                return list;
            }

            var dawnCall = AccessTools.Method(typeof(DawnHelper), nameof(DawnHelper.GetHoursUntilDawn));

            int rewritten = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];
                if (!ins.Calls(runMethod)) continue;

                // SleepRunner.Run takes (EntityPlayerLocal, int). The int (hours)
                // is the second arg, pushed last, so it is the instruction
                // immediately before the call. Replace the literal 8 with our
                // dynamic helper call.
                if (i == 0) continue;
                var prev = list[i - 1];
                bool isEight =
                    prev.opcode == OpCodes.Ldc_I4_8 ||
                    (prev.opcode == OpCodes.Ldc_I4_S && prev.operand is sbyte sb && sb == 8) ||
                    (prev.opcode == OpCodes.Ldc_I4 && prev.operand is int iv && iv == 8);
                if (!isEight) continue;

                list[i - 1] = new CodeInstruction(OpCodes.Call, dawnCall)
                {
                    labels = prev.labels,
                    blocks = prev.blocks,
                };
                rewritten++;
            }

            if (rewritten > 0)
            {
                Log.Out($"[WakaSleepWindowAlign] Radial sleep hours redirected to DawnHelper in {rewritten} site(s).");
            }
            else
            {
                Log.Warning("[WakaSleepWindowAlign] Radial transpile found no literal-8 site before SleepRunner.Run; check Sleep Overhaul version.");
            }
            return list;
        }
    }
}

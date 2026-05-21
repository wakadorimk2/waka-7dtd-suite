using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace WakaSleepWindowAlign
{
    // Sleep Overhaul's XUiC_TFDSleepDialog clamps the picked hours to 1..8 in five
    // places: the four +/- button delegates (compiled as nested display-class
    // methods like XUiC_TFDSleepDialog+<>c__DisplayClass*_*::<Init>b__*) and OnOpen
    // itself. We want to extend the upper bound to MaxHours so the player can
    // sleep through the whole night (20:00->05:00 = 9 hours, plus margin).
    //
    // Strategy: transpile every method on XUiC_TFDSleepDialog and its nested types
    // and replace the literal 8 that immediately precedes a Mathf.Clamp(int,int,int)
    // call. This is surgical -- other 8 literals (e.g. font sizes, indices) are
    // untouched because we only rewrite the constant when followed by a Clamp call.
    [HarmonyPatch]
    public static class TFDSleepDialog_Clamp_Transpiler
    {
        public const int MaxHours = 12;

        public static bool Prepare()
        {
            return TargetMethods().Any();
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            var asm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a =>
                {
                    var n = a.GetName().Name;
                    return n == "Bedroll Sleeping" || n == "BedrollSleeping";
                });
            if (asm == null)
            {
                Log.Warning("[WakaSleepWindowAlign] Sleep Overhaul assembly not found for dialog clamp transpile.");
                yield break;
            }
            var dialogType = asm.GetType("XUiC_TFDSleepDialog");
            if (dialogType == null)
            {
                Log.Warning("[WakaSleepWindowAlign] XUiC_TFDSleepDialog type not found.");
                yield break;
            }

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (var m in dialogType.GetMethods(flags))
            {
                if (m.IsAbstract || m.GetMethodBody() == null) continue;
                yield return m;
            }
            foreach (var nested in dialogType.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var m in nested.GetMethods(flags))
                {
                    if (m.IsAbstract || m.GetMethodBody() == null) continue;
                    yield return m;
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var list = new List<CodeInstruction>(instructions);
            var clampInt = AccessTools.Method(typeof(Mathf), "Clamp", new[] { typeof(int), typeof(int), typeof(int) });
            if (clampInt == null) return list;

            int rewritten = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var ins = list[i];
                if (!ins.Calls(clampInt)) continue;

                // The third argument (max) is the last value pushed before the call.
                // It's the instruction immediately preceding the call site in IL.
                if (i == 0) continue;
                var prev = list[i - 1];
                if (prev.opcode == OpCodes.Ldc_I4_8)
                {
                    list[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, MaxHours)
                    {
                        labels = prev.labels,
                        blocks = prev.blocks,
                    };
                    rewritten++;
                }
                else if (prev.opcode == OpCodes.Ldc_I4_S && prev.operand is sbyte sb && sb == 8)
                {
                    list[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, MaxHours)
                    {
                        labels = prev.labels,
                        blocks = prev.blocks,
                    };
                    rewritten++;
                }
                else if (prev.opcode == OpCodes.Ldc_I4 && prev.operand is int iv && iv == 8)
                {
                    list[i - 1] = new CodeInstruction(OpCodes.Ldc_I4, MaxHours)
                    {
                        labels = prev.labels,
                        blocks = prev.blocks,
                    };
                    rewritten++;
                }
            }

            if (rewritten > 0)
            {
                Log.Out($"[WakaSleepWindowAlign] Dialog clamp upper bound rewritten in {rewritten} site(s) -> {MaxHours}h.");
            }
            return list;
        }
    }
}

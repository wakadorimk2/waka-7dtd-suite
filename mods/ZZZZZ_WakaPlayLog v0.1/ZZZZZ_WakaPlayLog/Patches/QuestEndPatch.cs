using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaPlayLog.Patches
{
    /// <summary>
    /// Postfix on Quest end. Follows the same target-resolution pattern
    /// as WakaQuestProgression: try CloseQuest, HandleEnd, OnComplete,
    /// OnFinished, TurnIn, Complete in order. First-arg of method
    /// represents the end-state ("Completed"/"Failed"/"Cancelled").
    /// </summary>
    public static class QuestEndPatchTarget
    {
        public static MethodInfo Target;
        public static bool Ready;

        public static void Initialize()
        {
            try
            {
                var t = AccessTools.TypeByName("Quest");
                if (t == null) return;
                Target = AccessTools.Method(t, "CloseQuest")
                       ?? AccessTools.Method(t, "HandleEnd")
                       ?? AccessTools.Method(t, "OnComplete")
                       ?? AccessTools.Method(t, "OnFinished")
                       ?? AccessTools.Method(t, "TurnIn")
                       ?? AccessTools.Method(t, "Complete");
                if (Target == null) return;
                Ready = true;
                Log.Out($"[WakaPlayLog] QuestEnd hooked on Quest.{Target.Name}");
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] QuestEndPatchTarget.Initialize failed: {e.Message}");
            }
        }
    }

    [HarmonyPatch]
    public static class Quest_End_Patch
    {
        public static bool Prepare(MethodBase _)
        {
            if (!QuestEndPatchTarget.Ready) QuestEndPatchTarget.Initialize();
            return QuestEndPatchTarget.Ready;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!QuestEndPatchTarget.Ready) yield break;
            yield return QuestEndPatchTarget.Target;
        }

        public static void Postfix(object __instance, object[] __args)
            => QuestEvents.HandleQuestEnded(__instance, __args);
    }
}

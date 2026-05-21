using HarmonyLib;
using System.Reflection;

namespace WakaSleepWindowAlign
{
    // *** DORMANT PATCH (kept as insurance) ***
    // Sleep Overhaul hijacks Localization.Get with its own Prefix
    // (BedrollPatches.LocPrefix2) that hardcodes the label for
    // "blockcommand_sleep8_action" to "Sleep (8 hrs)" regardless of locale.
    // We Postfix that to "Sleep (Till Dawn)" so the label matches our dynamic
    // radial behavior (see RadialDynamicDawnPatch).
    //
    // HOWEVER: nobody actually queries Localization.Get with that key today --
    // see the dormant-patch note in RadialDynamicDawnPatch.cs. The radial path
    // is dead in shipped Sleep Overhaul v1.3.1, so this Postfix only matters if
    // a future update wires the radial up. Harmless no-op until then.
    //
    // Vanilla 7DTD 2.6 only exposes ONE single-key Get overload:
    //     public static string Get(string _key, bool _caseInsensitive = false)
    // Sleep Overhaul's decompiled code looks like it patches both Get(string) and
    // Get(string, bool), but Get(string) does not exist; AccessTools.Method
    // returned null and Sleep Overhaul silently skipped that patch via a null
    // guard. We patch the real (string, bool) overload only.
    //
    // We hardcode the English string here rather than going through the
    // Localization system: a recursive Localization.Get call from within a
    // Localization.Get Postfix risks infinite loops. If the player wants JP
    // text, edit the Label constant below.
    [HarmonyPatch]
    public static class Localization_Get_LabelPostfix
    {
        public const string LabelKey = "blockcommand_sleep8_action";
        public const string Label = "Sleep (Till Dawn)";

        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Localization), "Get", new[] { typeof(string), typeof(bool) });
        }

        public static void Postfix(string __0, ref string __result)
        {
            if (__0 == LabelKey)
            {
                __result = Label;
            }
        }
    }
}

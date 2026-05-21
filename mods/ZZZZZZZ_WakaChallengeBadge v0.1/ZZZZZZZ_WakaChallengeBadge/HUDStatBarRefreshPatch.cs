using HarmonyLib;

namespace WakaChallengeBadge
{
    // XUiC_HUDStatBar.Update only calls RefreshBindings when hasChanged()
    // returns true OR IsDirty is set. hasChanged() returns true only for
    // Health/Stamina/Water/Food/Stealth/Vehicle stat types via its switch
    // (XUiC_HUDStatBar.cs:216-243); rects that use the HUDStatBar controller
    // without a stat_type attribute (like CATUI's SkillPoints badge and our
    // ChallengesClaim badge) fall into the default branch and return false.
    //
    // That means their bindings are only re-evaluated when something else
    // accidentally flips IsDirty. Challenge state changes far more frequently
    // than skill-point increments, so the accidental refresh cadence is
    // visibly out of sync with the actual count.
    //
    // Instead of force-flipping IsDirty every tick, we only do so when a
    // challenge-state event recently fired (ChallengeBadgeDirtyState). That
    // keeps the badge count event-driven (cost: zero outside the short
    // post-event window) while still walking the vanilla refresh machinery
    // the way it's meant to be walked.
    [HarmonyPatch(typeof(XUiC_HUDStatBar), "Update")]
    public static class HUDStatBarRefreshPatch
    {
        public static void Postfix(XUiC_HUDStatBar __instance)
        {
            if (__instance == null) return;
            if (!ChallengeBadgeDirtyState.ShouldRefresh()) return;
            __instance.IsDirty = true;
        }
    }
}

using UnityEngine;

namespace WakaChallengeBadge
{
    // Event-driven dirty signal for the badge. A challenge-state-changing
    // event (HandleComplete, Redeem) calls MarkDirty(), and HUDStatBar's
    // Update Postfix consults ShouldRefresh() to decide whether to flip
    // IsDirty. Outside of these short windows the vanilla refresh cadence
    // is left alone, so the patch costs nothing per tick.
    //
    // We track Time.frameCount rather than a bool flag because multiple
    // XUiC_HUDStatBar instances tick in sequence within the same frame -
    // a single-shot bool would refresh only the first instance, leaving
    // the others (including our ChallengesClaim rect) un-refreshed.
    // Holding the dirty signal for ~2 frames lets all sibling bars pick
    // it up before it clears.
    public static class ChallengeBadgeDirtyState
    {
        // -1 = never marked. Treated as "not dirty" by ShouldRefresh().
        private static int dirtyFrame = -1;

        public static void MarkDirty()
        {
            dirtyFrame = Time.frameCount;
        }

        public static bool ShouldRefresh()
        {
            if (dirtyFrame < 0) return false;
            return Time.frameCount - dirtyFrame <= 2;
        }
    }
}

using Challenges;
using HarmonyLib;

namespace WakaChallengeBadge
{
    // Mark the badge dirty whenever a challenge transitions state in a way
    // that affects the claimable count:
    //   - HandleComplete: Active -> Completed (badge should +1)
    //   - Redeem:         Completed -> Redeemed (badge should -1)
    //
    // CompleteChallenge() internally invokes HandleComplete (and Redeem
    // when forceRedeem=true), so patching those two methods covers all
    // state-change paths without needing a third patch on CompleteChallenge.

    [HarmonyPatch(typeof(Challenge), nameof(Challenge.HandleComplete))]
    public static class ChallengeHandleCompleteDirtyPatch
    {
        public static void Postfix()
        {
            ChallengeBadgeDirtyState.MarkDirty();
        }
    }

    [HarmonyPatch(typeof(Challenge), nameof(Challenge.Redeem))]
    public static class ChallengeRedeemDirtyPatch
    {
        public static void Postfix()
        {
            ChallengeBadgeDirtyState.MarkDirty();
        }
    }
}

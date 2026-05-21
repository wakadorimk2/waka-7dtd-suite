using Challenges;
using HarmonyLib;

namespace WakaChallengeBadge
{
    // Exposes {CATUI_playerChallengesClaimable} as a string binding so the
    // XPath-appended rect in HUDLeftStatBars can read claimable-challenge
    // count. Vanilla has no challenge binding; ChallengeJournal.Challenges
    // is the only access path.
    //
    // Filter: c.ReadyToComplete && !c.ChallengeClass.RedeemAlways
    //   - ReadyToComplete matches vanilla's enableredeem binding
    //     (XUiC_ChallengeEntryDescriptionWindow.cs:443), i.e. exactly the set
    //     of challenges whose Complete button is enabled.
    //   - !RedeemAlways excludes vanilla's 'redeemChallenge' tutorial task
    //     (redeem_always="true") which sits in Active state permanently after
    //     one redemption and would otherwise pin the badge at >=1.
    //
    // Player access: via __instance.xui.playerUI.entityPlayer. We hit this
    // through the controller because every controller already carries a
    // valid xui reference once the HUD is up, and HUDStatBar (which polls
    // this binding) is part of the HUD. Going through LocalPlayerUI.primaryUI
    // looked cleaner but turned out to break the badge entirely in some
    // states - the controller path is what worked in the first build.
    //
    // No cache: O(~30) iteration per tick is negligible, and a cache only
    // introduces user-visible update lag.
    //
    // Belt-and-braces try/catch: a thrown exception in this Postfix would
    // propagate into vanilla XUiController.GetBindingValue (which logs and
    // rethrows), spamming the log. On any failure, return "0" so the badge
    // hides instead of breaking the binding chain.
    [HarmonyPatch(typeof(XUiController), nameof(XUiController.GetBindingValue))]
    public static class ChallengeBadgeBindingPatch
    {
        private const string BindingName = "CATUI_playerChallengesClaimable";

        public static void Postfix(ref bool __result, ref string _value, string _bindingName, XUiController __instance)
        {
            if (_bindingName != BindingName) return;
            if (__result) return;

            try
            {
                _value = GetClaimableCount(__instance);
            }
            catch
            {
                _value = "0";
            }
            __result = true;
        }

        private static string GetClaimableCount(XUiController controller)
        {
            var player = controller?.xui?.playerUI?.entityPlayer;
            var journal = player?.challengeJournal;
            var challenges = journal?.Challenges;
            if (challenges == null) return "0";

            int count = 0;
            for (int i = 0; i < challenges.Count; i++)
            {
                var c = challenges[i];
                if (c == null) continue;
                if (c.ChallengeClass != null && c.ChallengeClass.RedeemAlways) continue;
                // Skip challenges in groups hidden by a prerequisite group
                // (e.g. tutorial-locked Homesteading/Advanced Survival rows
                // unlocked only after reading the Duke's Note). Their entries
                // are not surfaced in the UI so they must not contribute to
                // the badge count either.
                if (c.ChallengeGroup != null && !c.ChallengeGroup.IsVisible(player)) continue;
                if (c.ReadyToComplete) count++;
            }
            return count.ToString();
        }
    }
}

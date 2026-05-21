using System;
using System.Collections.Generic;

namespace WakaPlayLog
{
    /// <summary>
    /// Quest-category event builders. Quest end (completed/failed/cancelled)
    /// is hooked from Quest.CloseQuest postfix. Quest acceptance is detected
    /// via 1 Hz diff on the player's QuestJournal.quests list. Scourge beacon
    /// started fires from POIScourge.ItemActionStartScourgeQuest postfix.
    /// Trader tier-up is polled by reading QuestJournal faction points.
    /// </summary>
    public static class QuestEvents
    {
        const string ScourgePrefix = "quest_scourge_infestation_t";

        public static void HandleQuestEnded(object questInstance, object[] args)
        {
            if (!LogWriter.IsActive || questInstance == null) return;
            try
            {
                if (!(questInstance is Quest q)) return;
                string id = q.ID;
                int tier = 0;
                try { tier = q.QuestClass?.DifficultyTier ?? 0; } catch { }

                string state = (args != null && args.Length >= 1 && args[0] != null)
                    ? args[0].ToString() : "Unknown";

                // Scourge beacon clear is a dedicated event when completed
                if (!string.IsNullOrEmpty(id) && id.StartsWith(ScourgePrefix, StringComparison.Ordinal)
                    && string.Equals(state, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    int scourgeTier = 0;
                    int.TryParse(id.Substring(ScourgePrefix.Length), out scourgeTier);
                    LogWriter.Write("quest", "scourge_beacon_cleared", "notable", new Dictionary<string, object>
                    {
                        { "quest_id", id },
                        { "scourge_tier", scourgeTier },
                    });
                    return;
                }

                string evt; string sev;
                if (string.Equals(state, "Completed", StringComparison.OrdinalIgnoreCase))
                { evt = "quest_completed"; sev = "notable"; }
                else if (string.Equals(state, "Failed", StringComparison.OrdinalIgnoreCase))
                { evt = "quest_failed"; sev = "rare"; }
                else
                { evt = "quest_cancelled"; sev = "normal"; }

                LogWriter.Write("quest", evt, sev, new Dictionary<string, object>
                {
                    { "quest_id", id },
                    { "tier", tier },
                    { "state", state },
                });
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] HandleQuestEnded failed: {e.Message}");
            }
        }

        public static void HandleScourgeBeaconStarted(string questId, int tier)
        {
            if (!LogWriter.IsActive) return;
            LogWriter.Write("quest", "scourge_beacon_started", "notable", new Dictionary<string, object>
            {
                { "quest_id", questId },
                { "scourge_tier", tier },
            });
        }

        public static void HandleTraderTrade(string action, int totalValue, string traderName)
        {
            if (!LogWriter.IsActive) return;
            if (totalValue < 1000) return; // threshold for "high value"
            LogWriter.Write("quest", "trader_high_value_trade", "notable", new Dictionary<string, object>
            {
                { "action", action },
                { "value", totalValue },
                { "trader", traderName },
            });
        }

        public static void PollQuestList(EntityPlayerLocal player)
        {
            if (player?.QuestJournal?.quests == null) return;
            try
            {
                var current = new HashSet<string>();
                foreach (var q in player.QuestJournal.quests)
                {
                    if (q == null || string.IsNullOrEmpty(q.ID)) continue;
                    current.Add(q.ID);
                    if (GameState.AcceptedQuestIds.Count > 0 && !GameState.AcceptedQuestIds.Contains(q.ID))
                    {
                        // Newly accepted
                        int tier = 0;
                        try { tier = q.QuestClass?.DifficultyTier ?? 0; } catch { }
                        LogWriter.Write("quest", "quest_accepted", "notable", new Dictionary<string, object>
                        {
                            { "quest_id", q.ID },
                            { "tier", tier },
                        });
                    }
                }
                GameState.AcceptedQuestIds.Clear();
                foreach (var id in current) GameState.AcceptedQuestIds.Add(id);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] PollQuestList failed: {e.Message}");
            }
        }

        public static void PollMoneyDelta(EntityPlayerLocal player)
        {
            if (player == null) return;
            try
            {
                int money = GetCurrencyCount(player);
                if (GameState.HasLastMoney)
                {
                    int delta = money - GameState.LastMoney;
                    if (Math.Abs(delta) >= 1000)
                    {
                        LogWriter.Write("quest", "trader_high_value_trade", "notable", new Dictionary<string, object>
                        {
                            { "delta", delta },
                            { "direction", delta > 0 ? "income" : "spent" },
                            { "balance_after", money },
                        });
                    }
                }
                GameState.LastMoney = money;
                GameState.HasLastMoney = true;
            }
            catch { }
        }

        static int GetCurrencyCount(EntityPlayer player)
        {
            try
            {
                var cls = ItemClass.GetItemClass("casinoCoin");
                if (cls == null) return 0;
                var iv = new ItemValue(cls.Id);
                int total = 0;
                if (player.bag != null) total += player.bag.GetItemCount(iv);
                if (player.inventory != null) total += player.inventory.GetItemCount(iv);
                return total;
            }
            catch { return 0; }
        }

        public static void PollTraderTier(EntityPlayerLocal player)
        {
            if (player?.QuestJournal == null) return;
            try
            {
                var journal = player.QuestJournal;
                // QuestFactionPoints: dict-like or list, varies by version.
                // Use reflection-tolerant approach via journal.GetTraderTier if present.
                var t = journal.GetType();
                var method = t.GetMethod("GetTraderTier", new[] { typeof(string) });
                if (method == null) return;

                // Iterate known faction keys we care about. 7DTD has a small fixed set:
                string[] factions = { "trader", "duke", "jen", "rekt", "bob", "joel", "hugh" };
                foreach (var f in factions)
                {
                    int tier;
                    try { tier = (int)method.Invoke(journal, new object[] { f }); }
                    catch { continue; }
                    int prev;
                    if (!GameState.LastFactionTier.TryGetValue(f, out prev)) prev = -1;
                    if (tier > prev && prev >= 0)
                    {
                        LogWriter.Write("quest", "trader_tier_up", "rare", new Dictionary<string, object>
                        {
                            { "faction", f },
                            { "tier", tier },
                            { "prev_tier", prev },
                        });
                    }
                    GameState.LastFactionTier[f] = tier;
                }
            }
            catch { }
        }
    }
}

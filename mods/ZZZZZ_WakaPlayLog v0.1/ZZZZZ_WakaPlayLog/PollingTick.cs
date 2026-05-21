using System;

namespace WakaPlayLog
{
    /// <summary>
    /// Runs at 1 Hz from ModEvents.GameUpdate. Polls the player for state
    /// that requires diff/threshold detection: day change, biome/POI entry,
    /// HP threshold, equipment broken, quest list diff, trader tier diff,
    /// session distance accumulation, large-step (fast travel) detection.
    /// </summary>
    public static class PollingTick
    {
        public static void Run()
        {
            try
            {
                var player = GameManager.Instance?.World?.GetPrimaryPlayer();
                if (player == null) return;

                MovementEvents.PollDayChange();
                MovementEvents.PollPositionAndBiome(player);
                CombatEvents.PollCriticalHP(player);
                CombatEvents.PollEquipmentBroken(player);
                QuestEvents.PollQuestList(player);
                QuestEvents.PollTraderTier(player);
                QuestEvents.PollMoneyDelta(player);

                // 1Hz flush so the session ndjson stays tail-able by
                // chill-assistant. Crash-time loss bounded to ~1 sec.
                LogWriter.Flush();
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] PollingTick failed: {e.Message}");
            }
        }
    }
}

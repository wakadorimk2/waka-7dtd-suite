using System;
using System.Collections.Generic;
using UnityEngine;

namespace WakaPlayLog
{
    /// <summary>
    /// Session-scoped state for first-visit detection, threshold latching,
    /// and diff-based event detection (quests/trader tier). Reset on
    /// session_start. Polling cadence throttled to 1 Hz.
    /// </summary>
    public static class GameState
    {
        public static DateTime SessionStartedAt;
        public static int LastKnownDay = -1;
        public static string LastKnownBiome;
        public static string LastKnownPOI;
        public static readonly HashSet<string> VisitedPOIs = new HashSet<string>();
        public static readonly HashSet<string> VisitedBiomes = new HashSet<string>();
        public static readonly HashSet<int> BrokenItemKeys = new HashSet<int>();
        public static readonly HashSet<string> AcceptedQuestIds = new HashSet<string>();
        public static Vector3 LastPosition = Vector3.zero;
        public static bool HasLastPosition;
        public static double SessionDistance;
        public static int Deaths;
        public static bool CriticalHPLatched;
        public static readonly Dictionary<string, int> LastFactionTier = new Dictionary<string, int>();
        public static int LastMoney;
        public static bool HasLastMoney;

        static float _lastPollTime = -1f;
        const float PollIntervalSec = 1.0f;

        public static void Reset()
        {
            SessionStartedAt = DateTime.Now;
            LastKnownDay = -1;
            LastKnownBiome = null;
            LastKnownPOI = null;
            VisitedPOIs.Clear();
            VisitedBiomes.Clear();
            BrokenItemKeys.Clear();
            AcceptedQuestIds.Clear();
            LastPosition = Vector3.zero;
            HasLastPosition = false;
            SessionDistance = 0.0;
            Deaths = 0;
            CriticalHPLatched = false;
            LastFactionTier.Clear();
            LastMoney = 0;
            HasLastMoney = false;
            _lastPollTime = -1f;
        }

        public static bool ShouldPollNow()
        {
            float t = Time.realtimeSinceStartup;
            if (_lastPollTime < 0f || t - _lastPollTime >= PollIntervalSec)
            {
                _lastPollTime = t;
                return true;
            }
            return false;
        }
    }

    public static class GameTime
    {
        public static string Format()
        {
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return "Day ? --:--";
                ulong t = world.GetWorldTime();
                int day = (int)(t / 24000UL) + 1;
                int hour = (int)((t % 24000UL) / 1000UL);
                int minute = (int)(((t % 24000UL) % 1000UL) * 60UL / 1000UL);
                return $"Day {day} {hour:D2}:{minute:D2}";
            }
            catch
            {
                return "Day ? --:--";
            }
        }

        public static int CurrentDay()
        {
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return -1;
                return (int)(world.GetWorldTime() / 24000UL) + 1;
            }
            catch
            {
                return -1;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WakaPlayLog
{
    /// <summary>
    /// Movement-category event builders. Day change, biome entry, POI
    /// first-visit detection, fast-travel detection (large position jump),
    /// session distance accumulation, session-end summary.
    /// </summary>
    public static class MovementEvents
    {
        const float FastTravelJumpMeters = 80f;

        public static void PollDayChange()
        {
            int day = GameTime.CurrentDay();
            if (day < 1) return;
            if (GameState.LastKnownDay < 0)
            {
                GameState.LastKnownDay = day;
                return;
            }
            if (day != GameState.LastKnownDay)
            {
                int prev = GameState.LastKnownDay;
                GameState.LastKnownDay = day;
                LogWriter.Write("movement", "day_started", "notable", new Dictionary<string, object>
                {
                    { "day", day },
                    { "prev_day", prev },
                });
            }
        }

        public static void PollPositionAndBiome(EntityPlayerLocal player)
        {
            if (player == null) return;
            try
            {
                var pos = player.position;

                // Distance accumulation + fast travel detection
                if (GameState.HasLastPosition)
                {
                    float d = Vector3.Distance(GameState.LastPosition, pos);
                    if (d >= FastTravelJumpMeters)
                    {
                        LogWriter.Write("movement", "fast_travel_used", "normal", new Dictionary<string, object>
                        {
                            { "from", new int[] { (int)GameState.LastPosition.x, (int)GameState.LastPosition.y, (int)GameState.LastPosition.z } },
                            { "to", new int[] { (int)pos.x, (int)pos.y, (int)pos.z } },
                            { "jump_m", (int)d },
                        });
                    }
                    else
                    {
                        GameState.SessionDistance += d;
                    }
                }
                GameState.LastPosition = pos;
                GameState.HasLastPosition = true;

                // Biome
                string biome = null;
                try { biome = player.biomeStandingOn?.m_sBiomeName; } catch { }
                if (!string.IsNullOrEmpty(biome) && biome != GameState.LastKnownBiome)
                {
                    bool firstTime = !GameState.VisitedBiomes.Contains(biome);
                    GameState.VisitedBiomes.Add(biome);
                    string sev = firstTime ? "notable" : "normal";
                    LogWriter.Write("movement", "biome_crossed", sev, new Dictionary<string, object>
                    {
                        { "biome", biome },
                        { "first_time", firstTime },
                        { "prev_biome", GameState.LastKnownBiome },
                    });
                    GameState.LastKnownBiome = biome;
                }

                // POI first visit
                string poiName = TryGetPOIName(player);
                if (!string.IsNullOrEmpty(poiName) && poiName != GameState.LastKnownPOI)
                {
                    if (!GameState.VisitedPOIs.Contains(poiName))
                    {
                        GameState.VisitedPOIs.Add(poiName);
                        int tier = TryGetPOITier(player);
                        LogWriter.Write("movement", "poi_first_visit", "notable", new Dictionary<string, object>
                        {
                            { "poi", poiName },
                            { "tier", tier },
                            { "biome", biome },
                        });
                    }
                    GameState.LastKnownPOI = poiName;
                }
                else if (string.IsNullOrEmpty(poiName))
                {
                    GameState.LastKnownPOI = null;
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] PollPositionAndBiome failed: {e.Message}");
            }
        }

        public static void HandleSessionEnd()
        {
            if (!LogWriter.IsActive) return;
            try
            {
                LogWriter.Write("movement", "session_distance_summary", "trivial", new Dictionary<string, object>
                {
                    { "distance_m", (int)GameState.SessionDistance },
                    { "visited_poi_count", GameState.VisitedPOIs.Count },
                    { "visited_biome_count", GameState.VisitedBiomes.Count },
                });
            }
            catch { }
        }

        static string TryGetPOIName(EntityPlayerLocal player)
        {
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return null;
                var pos = new Vector3i(
                    Mathf.FloorToInt(player.position.x),
                    Mathf.FloorToInt(player.position.y),
                    Mathf.FloorToInt(player.position.z));
                var dpd = world.ChunkClusters?[0]?.ChunkProvider?.GetDynamicPrefabDecorator();
                if (dpd == null) return null;
                var inst = dpd.GetPrefabAtPosition(pos);
                return inst?.name;
            }
            catch { return null; }
        }

        static int TryGetPOITier(EntityPlayerLocal player)
        {
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return 0;
                var pos = new Vector3i(
                    Mathf.FloorToInt(player.position.x),
                    Mathf.FloorToInt(player.position.y),
                    Mathf.FloorToInt(player.position.z));
                var dpd = world.ChunkClusters?[0]?.ChunkProvider?.GetDynamicPrefabDecorator();
                if (dpd == null) return 0;
                var inst = dpd.GetPrefabAtPosition(pos);
                return inst?.prefab?.DifficultyTier ?? 0;
            }
            catch { return 0; }
        }
    }
}

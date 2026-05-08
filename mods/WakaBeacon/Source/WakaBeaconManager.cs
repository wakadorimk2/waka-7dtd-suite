using System;
using System.Collections.Generic;
using UnityEngine;

namespace WakaBeacon
{
    public static class WakaBeaconManager
    {
        const string NavObjectClassName = "waka_beacon";
        const string BaseLabel = "Beacon";

        static readonly Dictionary<Vector3i, int> beaconEntityIds = new Dictionary<Vector3i, int>();
        static int nextBeaconId = int.MinValue + 1;
        static int frameCounter;

        public static void Register(Vector3i pos)
        {
            if (beaconEntityIds.ContainsKey(pos)) return;
            var mgr = NavObjectManager.Instance;
            if (mgr == null) return;

            try
            {
                var worldPos = new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f);
                int id = nextBeaconId++;
                NavObject nav = mgr.RegisterNavObject(NavObjectClassName, worldPos, null, false, id, null);
                if (nav == null)
                {
                    Log.Warning($"[WakaBeacon] RegisterNavObject returned null at {pos}");
                    return;
                }
                nav.name = BaseLabel;
                nav.usingLocalizationId = false;
                beaconEntityIds[pos] = id;
                Log.Out($"[WakaBeacon] Registered at {pos} (id={id})");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaBeacon] Register failed at {pos}: {e}");
            }
        }

        public static void Unregister(Vector3i pos)
        {
            if (!beaconEntityIds.TryGetValue(pos, out int id)) return;
            beaconEntityIds.Remove(pos);
            try
            {
                NavObjectManager.Instance?.UnRegisterNavObjectByEntityID(id);
                Log.Out($"[WakaBeacon] Unregistered at {pos} (id={id})");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaBeacon] Unregister failed at {pos}: {e}");
            }
        }

        public static void OnGameShutdown(ref ModEvents.SGameShutdownData _data)
        {
            var mgr = NavObjectManager.Instance;
            if (mgr != null)
            {
                foreach (var id in beaconEntityIds.Values)
                {
                    try { mgr.UnRegisterNavObjectByEntityID(id); } catch { }
                }
            }
            beaconEntityIds.Clear();
        }

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData _data)
        {
            frameCounter++;
            if (frameCounter % 12 != 0) return;
            if (beaconEntityIds.Count == 0) return;

            try
            {
                var player = GameManager.Instance?.World?.GetPrimaryPlayer();
                if (player == null) return;
                int playerY = Mathf.RoundToInt(player.position.y);
                var mgr = NavObjectManager.Instance;
                if (mgr == null) return;

                foreach (var kv in beaconEntityIds)
                {
                    int dy = kv.Key.y - playerY;
                    string label = dy == 0
                        ? BaseLabel
                        : dy > 0
                            ? $"{BaseLabel} ▲{dy}m"
                            : $"{BaseLabel} ▼{-dy}m";

                    var nav = mgr.GetNavObjectByEntityID(kv.Value);
                    if (nav != null && nav.name != label)
                    {
                        nav.name = label;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"[WakaBeacon] UpdateLabels failed: {e}");
            }
        }
    }
}

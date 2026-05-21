using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace WakaPlayLog.Patches
{
    /// <summary>
    /// Postfix on POIScourge.ItemActionStartScourgeQuest methods that take
    /// an ItemActionData argument. Reads the QuestID via a reflection
    /// path (DynamicProperties), reads the POI tier from the player
    /// position, and emits scourge_beacon_started. Mirrors the bridge
    /// pattern from WakaQuestProgression but read-only.
    /// </summary>
    public static class ScourgeBeaconBridge
    {
        public const string TargetTypeName = "POIScourge.ItemActionStartScourgeQuest";
        public static Type ItemActionType;
        public static FieldInfo PropertiesField;
        public static MethodInfo DpGetStringMethod;
        public static bool Ready;

        public static void Initialize()
        {
            try
            {
                ItemActionType = AccessTools.TypeByName(TargetTypeName);
                if (ItemActionType == null) return;

                PropertiesField = FindFieldByName(ItemActionType, "Properties");
                if (PropertiesField == null) return;

                var dpType = PropertiesField.FieldType;
                DpGetStringMethod = AccessTools.Method(dpType, "GetString", new[] { typeof(string) });
                if (DpGetStringMethod == null) return;

                Ready = true;
                Log.Out("[WakaPlayLog] ScourgeBeaconBridge ready");
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] ScourgeBeaconBridge init failed: {e.Message}");
            }
        }

        public static string ReadQuestId(object actionInstance)
        {
            try
            {
                var props = PropertiesField?.GetValue(actionInstance);
                if (props == null) return null;
                return DpGetStringMethod?.Invoke(props, new object[] { "QuestID" }) as string;
            }
            catch { return null; }
        }

        static FieldInfo FindFieldByName(Type t, string name)
        {
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
            {
                var f = cur.GetField(name, bf | BindingFlags.DeclaredOnly);
                if (f != null) return f;
            }
            return null;
        }
    }

    [HarmonyPatch]
    public static class ItemActionStartScourgeQuest_Patch
    {
        public static bool Prepare(MethodBase _)
        {
            if (!ScourgeBeaconBridge.Ready) ScourgeBeaconBridge.Initialize();
            return ScourgeBeaconBridge.Ready;
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            if (!ScourgeBeaconBridge.Ready) yield break;
            var t = ScourgeBeaconBridge.ItemActionType;
            const BindingFlags bf = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var m in t.GetMethods(bf))
            {
                if (m.IsAbstract) continue;
                if (m.Name != "ExecuteAction" && m.Name != "OnHoldingItemAction" && m.Name != "StartHolding") continue;
                yield return m;
            }
        }

        public static void Postfix(object __instance)
        {
            try
            {
                var id = ScourgeBeaconBridge.ReadQuestId(__instance);
                if (string.IsNullOrEmpty(id)) return;
                int tier = TryGetTier();
                QuestEvents.HandleScourgeBeaconStarted(id, tier);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaPlayLog] ScourgeBeaconStart Postfix failed: {e.Message}");
            }
        }

        static int TryGetTier()
        {
            try
            {
                var player = GameManager.Instance?.World?.GetPrimaryPlayer();
                if (player == null) return 0;
                var pos = new Vector3i(
                    UnityEngine.Mathf.FloorToInt(player.position.x),
                    UnityEngine.Mathf.FloorToInt(player.position.y),
                    UnityEngine.Mathf.FloorToInt(player.position.z));
                var dpd = GameManager.Instance.World.ChunkClusters?[0]?.ChunkProvider?.GetDynamicPrefabDecorator();
                if (dpd == null) return 0;
                var inst = dpd.GetPrefabAtPosition(pos);
                return inst?.prefab?.DifficultyTier ?? 0;
            }
            catch { return 0; }
        }
    }
}

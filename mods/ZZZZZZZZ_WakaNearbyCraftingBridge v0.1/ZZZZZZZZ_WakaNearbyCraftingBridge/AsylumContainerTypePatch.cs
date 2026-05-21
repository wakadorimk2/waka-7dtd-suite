using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace WakaNearbyCraftingBridge
{
    [HarmonyPatch]
    public static class AsylumContainerTypePatch
    {
        private static readonly string[] WakaStorageBlocks =
        {
            "wakaIronStorage",
            "wakaSteelStorage",
            "wakaLogisticsContainer"
        };

        public static MethodBase TargetMethod()
        {
            Type scannerType = AccessTools.TypeByName("AsylumNearbyCrafting.ContainerScanner");
            return scannerType == null ? null : AccessTools.Method(scannerType, "IsValidContainerType");
        }

        public static void Postfix(string blockName, ref bool __result)
        {
            if (__result || string.IsNullOrEmpty(blockName) || !IsModdedContainersEnabled())
            {
                return;
            }

            foreach (string wakaStorageBlock in WakaStorageBlocks)
            {
                if (blockName.Equals(wakaStorageBlock, StringComparison.OrdinalIgnoreCase))
                {
                    __result = true;
                    return;
                }
            }
        }

        private static bool IsModdedContainersEnabled()
        {
            Type configType = AccessTools.TypeByName("AsylumNearbyCrafting.ConfigManager");
            PropertyInfo property = configType?.GetProperty("EnableModdedContainers", BindingFlags.Public | BindingFlags.Static);
            object value = property?.GetValue(null, null);
            return value is bool enabled && enabled;
        }
    }

    [HarmonyPatch]
    public static class AsylumContainerCountCachePatch
    {
        private const float CacheSeconds = 0.15f;
        private static readonly Dictionary<string, CacheEntry> CountCache = new Dictionary<string, CacheEntry>();

        public static MethodBase TargetMethod()
        {
            Type scannerType = AccessTools.TypeByName("AsylumNearbyCrafting.ContainerScanner");
            return scannerType == null ? null : AccessTools.Method(scannerType, "CountItemInContainers");
        }

        public static bool Prefix(List<ITileEntityLootable> containers, ItemValue itemValue, ref int __result)
        {
            if (containers == null || itemValue == null || itemValue.type <= 0)
            {
                return true;
            }

            string key = BuildCacheKey(containers, itemValue.type);
            if (CountCache.TryGetValue(key, out CacheEntry entry) && Time.realtimeSinceStartup - entry.Time < CacheSeconds)
            {
                __result = entry.Count;
                return false;
            }

            return true;
        }

        public static void Postfix(List<ITileEntityLootable> containers, ItemValue itemValue, int __result)
        {
            if (containers == null || itemValue == null || itemValue.type <= 0)
            {
                return;
            }

            if (CountCache.Count > 512)
            {
                CountCache.Clear();
            }

            CountCache[BuildCacheKey(containers, itemValue.type)] = new CacheEntry(__result, Time.realtimeSinceStartup);
        }

        private static string BuildCacheKey(List<ITileEntityLootable> containers, int itemType)
        {
            return $"{itemType}:{RuntimeHelpers.GetHashCode(containers)}";
        }

        private readonly struct CacheEntry
        {
            public CacheEntry(int count, float time)
            {
                Count = count;
                Time = time;
            }

            public int Count { get; }
            public float Time { get; }
        }
    }
}

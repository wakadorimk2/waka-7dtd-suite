using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
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
        private const float CacheSeconds = 0.5f;
        private const int MaxCacheEntries = 512;
        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private static readonly Dictionary<CacheKey, CacheEntry> CountCache = new Dictionary<CacheKey, CacheEntry>();

        [ThreadStatic]
        private static List<ulong> containerIds;

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

            CacheKey key = BuildCacheKey(containers, itemValue.type);
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

            if (CountCache.Count > MaxCacheEntries)
            {
                CountCache.Clear();
            }

            CountCache[BuildCacheKey(containers, itemValue.type)] = new CacheEntry(__result, Time.realtimeSinceStartup);
        }

        private static CacheKey BuildCacheKey(List<ITileEntityLootable> containers, int itemType)
        {
            List<ulong> ids = containerIds ?? (containerIds = new List<ulong>(64));
            ids.Clear();

            for (int i = 0; i < containers.Count; i++)
            {
                ITileEntityLootable container = containers[i];
                if (container == null)
                {
                    continue;
                }

                ids.Add(GetContainerId(container));
            }

            ids.Sort();

            ulong signature = FnvOffset;
            for (int i = 0; i < ids.Count; i++)
            {
                signature ^= ids[i];
                signature *= FnvPrime;
            }

            return new CacheKey(itemType, ids.Count, signature);
        }

        private static ulong GetContainerId(ITileEntityLootable container)
        {
            if (container is TileEntity tileEntity)
            {
                return BuildContainerId(tileEntity.GetClrIdx(), tileEntity.ToWorldPos(), tileEntity.EntityId);
            }

            if (container is TEFeatureAbs feature)
            {
                return BuildContainerId(feature.GetClrIdx(), feature.ToWorldPos(), feature.EntityId);
            }

            return (ulong)container.GetHashCode();
        }

        private static ulong BuildContainerId(int clrIdx, Vector3i worldPos, int entityId)
        {
            ulong hash = FnvOffset;
            hash = Mix(hash, (uint)clrIdx);
            hash = Mix(hash, (uint)worldPos.x);
            hash = Mix(hash, (uint)worldPos.y);
            hash = Mix(hash, (uint)worldPos.z);
            hash = Mix(hash, (uint)entityId);
            return hash;
        }

        private static ulong Mix(ulong hash, uint value)
        {
            hash ^= value;
            return hash * FnvPrime;
        }

        private readonly struct CacheKey : IEquatable<CacheKey>
        {
            public CacheKey(int itemType, int containerCount, ulong containerSignature)
            {
                ItemType = itemType;
                ContainerCount = containerCount;
                ContainerSignature = containerSignature;
            }

            public int ItemType { get; }
            public int ContainerCount { get; }
            public ulong ContainerSignature { get; }

            public bool Equals(CacheKey other)
            {
                return ItemType == other.ItemType
                    && ContainerCount == other.ContainerCount
                    && ContainerSignature == other.ContainerSignature;
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ItemType;
                    hash = (hash * 397) ^ ContainerCount;
                    hash = (hash * 397) ^ ContainerSignature.GetHashCode();
                    return hash;
                }
            }
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

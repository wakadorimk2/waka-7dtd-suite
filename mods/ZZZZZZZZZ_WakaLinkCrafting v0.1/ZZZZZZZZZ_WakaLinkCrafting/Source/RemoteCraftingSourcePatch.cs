using System.Collections.Generic;
using HarmonyLib;
using SCore.Features.RemoteCrafting.Scripts;
using UnityEngine;

namespace WakaLinkCrafting
{
    [HarmonyPatch(typeof(RemoteCraftingUtils), nameof(RemoteCraftingUtils.GetTileEntities), new[] { typeof(EntityAlive), typeof(float), typeof(bool) })]
    public static class RemoteCraftingSourcePatch
    {
        const string LinkedLootList = "wakaLinkedLogisticsContainer";
        const string WorkbenchName = "workbench";
        static readonly Dictionary<int, SelectionLogState> LastLoggedSelection = new Dictionary<int, SelectionLogState>();

        public static void Postfix(EntityAlive player, bool forRepairs, ref List<TileEntity> __result)
        {
            if (__result == null || __result.Count == 0) return;

            if (forRepairs || !IsWorkbenchContext(player))
            {
                __result.Clear();
                return;
            }

            var nearest = FindNearestLinkedContainer(player, __result);
            __result.Clear();
            if (nearest == null) return;

            __result.Add(nearest);
            LogSelectedContainer(player, nearest);
        }

        static bool IsWorkbenchContext(EntityAlive player)
        {
            if (!(player is EntityPlayerLocal localPlayer)) return false;
            var workstation = localPlayer.PlayerUI?.xui?.currentWorkstation;
            return workstation == WorkbenchName;
        }

        static TileEntity FindNearestLinkedContainer(EntityAlive player, List<TileEntity> tileEntities)
        {
            TileEntity nearest = null;
            var bestDistanceSq = float.MaxValue;
            var bestPos = Vector3i.zero;

            foreach (var tileEntity in tileEntities)
            {
                if (tileEntity == null) continue;
                if (!tileEntity.TryGetSelfOrFeature<ITileEntityLootable>(out var lootTileEntity)) continue;
                if (lootTileEntity.lootListName != LinkedLootList) continue;

                var pos = tileEntity.ToWorldPos();
                var distanceSq = (pos.ToVector3() - player.position).sqrMagnitude;
                if (nearest != null && !IsBetterCandidate(distanceSq, pos, bestDistanceSq, bestPos)) continue;

                nearest = tileEntity;
                bestDistanceSq = distanceSq;
                bestPos = pos;
            }

            return nearest;
        }

        static bool IsBetterCandidate(float distanceSq, Vector3i pos, float bestDistanceSq, Vector3i bestPos)
        {
            const float Epsilon = 0.0001f;
            if (distanceSq < bestDistanceSq - Epsilon) return true;
            if (distanceSq > bestDistanceSq + Epsilon) return false;
            if (pos.x != bestPos.x) return pos.x < bestPos.x;
            if (pos.y != bestPos.y) return pos.y < bestPos.y;
            return pos.z < bestPos.z;
        }

        static void LogSelectedContainer(EntityAlive player, TileEntity selected)
        {
            var pos = selected.ToWorldPos();
            var playerId = player.entityId;
            var now = Time.realtimeSinceStartup;

            if (LastLoggedSelection.TryGetValue(playerId, out var last) &&
                last.Position == pos &&
                now - last.Time < 10f)
            {
                return;
            }

            LastLoggedSelection[playerId] = new SelectionLogState(pos, now);
            Log.Out($"[WakaLinkCrafting] Remote crafting source for player {playerId}: {LinkedLootList} at {pos}");
        }

        readonly struct SelectionLogState
        {
            public SelectionLogState(Vector3i position, float time)
            {
                Position = position;
                Time = time;
            }

            public readonly Vector3i Position;
            public readonly float Time;
        }
    }
}

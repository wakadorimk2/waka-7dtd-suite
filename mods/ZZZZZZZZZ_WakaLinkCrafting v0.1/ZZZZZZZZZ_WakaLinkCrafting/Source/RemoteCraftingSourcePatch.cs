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
        static readonly HashSet<string> AllowedWorkstations = new HashSet<string>
        {
            "workbench",
            "campfire",
            "chemistryStation",
            "cementMixer",
            "ammopress",
            "researchbench"
        };
        static readonly Dictionary<int, SelectionLogState> LastLoggedSelection = new Dictionary<int, SelectionLogState>();

        public static void Postfix(EntityAlive player, bool forRepairs, ref List<TileEntity> __result)
        {
            if (__result == null || __result.Count == 0) return;

            if (forRepairs || !IsAllowedWorkstationContext(player))
            {
                __result.Clear();
                return;
            }

            var linkedContainers = FindLinkedContainersOrdered(player, __result);
            __result.Clear();
            if (linkedContainers.Count == 0) return;

            __result.AddRange(linkedContainers);
            LogSelectedContainers(player, linkedContainers);
        }

        static bool IsAllowedWorkstationContext(EntityAlive player)
        {
            if (!(player is EntityPlayerLocal localPlayer)) return false;
            var workstation = localPlayer.PlayerUI?.xui?.currentWorkstation;
            return !string.IsNullOrEmpty(workstation) && AllowedWorkstations.Contains(workstation);
        }

        static List<TileEntity> FindLinkedContainersOrdered(EntityAlive player, List<TileEntity> tileEntities)
        {
            var candidates = new List<LinkedContainerCandidate>();

            foreach (var tileEntity in tileEntities)
            {
                if (tileEntity == null) continue;
                if (!tileEntity.TryGetSelfOrFeature<ITileEntityLootable>(out var lootTileEntity)) continue;
                if (lootTileEntity.lootListName != LinkedLootList) continue;

                var pos = tileEntity.ToWorldPos();
                var distanceSq = (pos.ToVector3() - player.position).sqrMagnitude;
                candidates.Add(new LinkedContainerCandidate(tileEntity, pos, distanceSq));
            }

            candidates.Sort(CompareCandidates);

            var result = new List<TileEntity>(candidates.Count);
            foreach (var candidate in candidates)
            {
                result.Add(candidate.TileEntity);
            }

            return result;
        }

        static int CompareCandidates(LinkedContainerCandidate left, LinkedContainerCandidate right)
        {
            const float Epsilon = 0.0001f;
            if (left.DistanceSq < right.DistanceSq - Epsilon) return -1;
            if (left.DistanceSq > right.DistanceSq + Epsilon) return 1;
            if (left.Position.x != right.Position.x) return left.Position.x.CompareTo(right.Position.x);
            if (left.Position.y != right.Position.y) return left.Position.y.CompareTo(right.Position.y);
            return left.Position.z.CompareTo(right.Position.z);
        }

        static void LogSelectedContainers(EntityAlive player, List<TileEntity> selected)
        {
            var playerId = player.entityId;
            var now = Time.realtimeSinceStartup;
            var positions = BuildPositionList(selected);

            if (LastLoggedSelection.TryGetValue(playerId, out var last) &&
                last.Positions == positions &&
                now - last.Time < 10f)
            {
                return;
            }

            LastLoggedSelection[playerId] = new SelectionLogState(positions, now);
            Log.Out($"[WakaLinkCrafting] Remote crafting sources for player {playerId}: {selected.Count} {LinkedLootList} crates, nearest first: {positions}");
        }

        static string BuildPositionList(List<TileEntity> selected)
        {
            var positions = new List<string>(selected.Count);
            foreach (var tileEntity in selected)
            {
                positions.Add(tileEntity.ToWorldPos().ToString());
            }

            return string.Join(", ", positions);
        }

        readonly struct LinkedContainerCandidate
        {
            public LinkedContainerCandidate(TileEntity tileEntity, Vector3i position, float distanceSq)
            {
                TileEntity = tileEntity;
                Position = position;
                DistanceSq = distanceSq;
            }

            public readonly TileEntity TileEntity;
            public readonly Vector3i Position;
            public readonly float DistanceSq;
        }

        readonly struct SelectionLogState
        {
            public SelectionLogState(string positions, float time)
            {
                Positions = positions;
                Time = time;
            }

            public readonly string Positions;
            public readonly float Time;
        }
    }
}

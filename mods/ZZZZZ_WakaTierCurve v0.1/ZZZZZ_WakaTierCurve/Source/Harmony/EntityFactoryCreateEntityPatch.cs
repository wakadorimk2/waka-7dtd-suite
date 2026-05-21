using System;
using HarmonyLib;

namespace WakaTierCurve.HarmonyPatches
{
    /// <summary>
    /// Single chokepoint hook: every entity creation in vanilla 7DTD funnels into
    /// `EntityFactory.CreateEntity(EntityCreationData)`. All other CreateEntity
    /// overloads call this one, and every spawn path (biome wandering, POI sleeper,
    /// blood moon, WalkerSim, MinEvent, chunk restore from region files) ultimately
    /// hits it. Hooking here gives "1 entity creation = 1 swap" with no path
    /// duplication and no bypass — including the previously-uncovered chunk-restore
    /// path that resurrected pre-patch Brutals from region save files.
    ///
    /// Replaces three Phase 3 hooks:
    ///   EntityGroups.GetRandomFromGroup (vanilla spawn group lookup)
    ///   WalkerSim.SpawnManager.GetRandomFromGroup{,List} (WalkerSim new agent)
    ///   WalkerSim.SpawnManager.SpawnAgent (WalkerSim cached agent)
    ///
    /// EntityCreationData is a reference type, so we mutate its `entityClass`
    /// field in place and the body of CreateEntity proceeds with the swapped id.
    /// </summary>
    [HarmonyPatch(typeof(EntityFactory), nameof(EntityFactory.CreateEntity), new[] { typeof(EntityCreationData) })]
    public static class EntityFactoryCreateEntityPatch
    {
        private static int _callCount;
        private static int _swapCount;
        private static float _lastGameStageCheckTime = -999f;
        private static int _cachedGameStage = 1;

        [HarmonyPrefix]
        public static void Prefix(EntityCreationData _ecd)
        {
            if (_ecd == null) return;

            int origId = _ecd.entityClass;
            // NOTE: EntityClass.list keys are entityClassName.GetHashCode() — signed int32,
            // negatives are valid (e.g. BrutalWightTier6 = -1983319815). Only 0 means "no id".
            if (origId == 0) return;

            int inspectIdx = System.Threading.Interlocked.Increment(ref _callCount);

            try
            {
                if (!TierMapper.TryGetByClassId(origId, out var baseName, out var origTier))
                    return;

                var available = TierMapper.TiersFor(baseName);
                if (available == null) return;

                var rng = GameManager.Instance?.World?.GetGameRandom();
                if (rng == null) return;

                int gs = GetCurrentGameStage();
                int newTier = TierCurve.SampleTier(gs, rng, available);

                if (newTier <= 0 || newTier == origTier) return;

                if (!TierMapper.TryGetClassId(baseName, newTier, out int newId)) return;

                _ecd.entityClass = newId;
                System.Threading.Interlocked.Increment(ref _swapCount);
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaTierCurve] EF Prefix failed: {e.Message}");
            }
        }

        private static int GetCurrentGameStage()
        {
            try
            {
                float now = UnityEngine.Time.realtimeSinceStartup;
                if (now - _lastGameStageCheckTime < 1.0f)
                    return _cachedGameStage;

                var world = GameManager.Instance?.World;
                if (world == null) return _cachedGameStage;
                var players = world.GetPlayers();
                if (players == null || players.Count == 0) return _cachedGameStage;
                int max = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p == null) continue;
                    int gs = p.HighestPartyGameStage;
                    if (gs > max) max = gs;
                }
                _cachedGameStage = max > 0 ? max : 1;
                _lastGameStageCheckTime = now;
                return _cachedGameStage;
            }
            catch
            {
                return _cachedGameStage;
            }
        }
    }
}

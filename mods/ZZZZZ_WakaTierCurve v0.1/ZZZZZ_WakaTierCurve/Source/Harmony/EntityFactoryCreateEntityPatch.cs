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
        private static bool _firstCallLogged;

        [HarmonyPrefix]
        public static void Prefix(EntityCreationData _ecd)
        {
            if (_ecd == null) return;

            if (!_firstCallLogged)
            {
                _firstCallLogged = true;
                Log.Out($"[WakaTierCurve] EF FIRST Prefix call: entityClass={_ecd.entityClass}");
            }

            int origId = _ecd.entityClass;
            // NOTE: EntityClass.list keys are entityClassName.GetHashCode() — signed int32,
            // negatives are valid (e.g. BrutalWightTier6 = -1983319815). Only 0 means "no id".
            if (origId == 0) return;

            int inspectIdx = System.Threading.Interlocked.Increment(ref _callCount);
            bool verbose = inspectIdx <= 20;

            try
            {
                string origName = EntityClass.list.TryGetValue(origId, out var oc) ? oc?.entityClassName : "?";
                // Always log if the input is a Bloodfall variant — we need to see every T6+ input.
                bool isBloodfallInput = origName != null && (origName.StartsWith("Brutal") || origName.StartsWith("Alpha")
                    || origName.StartsWith("Prime") || origName.StartsWith("Apex") || origName.StartsWith("Torment")
                    || origName.StartsWith("Nightmare") || origName.StartsWith("Hellborn") || origName.StartsWith("Overlord")
                    || origName.StartsWith("Demigod") || origName.StartsWith("Bloodlord"));
                bool log = verbose || isBloodfallInput;

                if (!TierMapper.TryGetByClassId(origId, out var baseName, out var origTier))
                {
                    if (log)
                        Log.Out($"[WakaTierCurve] EF inspect#{inspectIdx}: id={origId}, name='{origName}', NOT tier-able (skipped)");
                    return;
                }

                var available = TierMapper.TiersFor(baseName);
                if (available == null)
                {
                    if (log)
                        Log.Out($"[WakaTierCurve] EF inspect#{inspectIdx}: id={origId}, name='{origName}', base={baseName}, NO tier map (skipped)");
                    return;
                }

                var rng = GameManager.Instance?.World?.GetGameRandom();
                if (rng == null)
                {
                    if (log)
                        Log.Out($"[WakaTierCurve] EF inspect#{inspectIdx}: id={origId}, name='{origName}', NO RNG (skipped)");
                    return;
                }

                int gs = GetCurrentGameStage();
                int newTier = TierCurve.SampleTier(gs, rng, available);

                if (log)
                {
                    var availList = string.Join(",", available.Keys);
                    Log.Out($"[WakaTierCurve] EF inspect#{inspectIdx}: id={origId}, name='{origName}', base={baseName}, origTier={origTier}, gs={gs}, available=[{availList}], curve picked T{newTier}");
                }

                if (newTier <= 0 || newTier == origTier) return;

                if (!TierMapper.TryGetClassId(baseName, newTier, out int newId))
                {
                    if (log)
                        Log.Out($"[WakaTierCurve] EF inspect#{inspectIdx}: target T{newTier} not defined for base={baseName} (swap canceled)");
                    return;
                }

                _ecd.entityClass = newId;
                int sc = System.Threading.Interlocked.Increment(ref _swapCount);

                // Always log Bloodfall→vanilla swaps, plus first 20 swaps for general visibility.
                if (sc <= 20 || isBloodfallInput)
                {
                    string newName = EntityClass.list.TryGetValue(newId, out var nc) ? nc?.entityClassName : "?";
                    Log.Out($"[WakaTierCurve] EF swap#{sc}: {origName}(T{origTier}) -> {newName}(T{newTier}) [base={baseName}, gs={gs}]");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaTierCurve] EF Prefix failed: {e.Message}");
            }
            finally
            {
                if (inspectIdx % 10 == 0)
                {
                    Log.Out($"[WakaTierCurve] EF Prefix calls={inspectIdx}, swaps={_swapCount}");
                }
            }
        }

        private static int GetCurrentGameStage()
        {
            try
            {
                var world = GameManager.Instance?.World;
                if (world == null) return 1;
                var players = world.GetPlayers();
                if (players == null || players.Count == 0) return 1;
                int max = 0;
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i];
                    if (p == null) continue;
                    int gs = p.HighestPartyGameStage;
                    if (gs > max) max = gs;
                }
                return max > 0 ? max : 1;
            }
            catch
            {
                return 1;
            }
        }
    }
}

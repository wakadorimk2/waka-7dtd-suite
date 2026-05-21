using System;
using HarmonyLib;

namespace WakaTierCurve.HarmonyPatches
{
    /// <summary>
    /// Observation-only Postfix on World.SpawnEntityInWorld(Entity).
    /// Logs every entity added to the world, regardless of spawn pipeline.
    /// Used to identify entities that bypass EntityFactory.CreateEntity
    /// (the Phase 4 chokepoint) — particularly the Wight/TomClark/FatCop/Lumberjack
    /// Brutal/Alpha entities that show up in screenshots but never appear as input
    /// to the CreateEntity Prefix.
    ///
    /// Bloodfall variant entries (name starts with Brutal/Alpha/Prime/...) are
    /// always logged with full detail. Other entities are throttled to verbose
    /// for the first 20 calls only.
    /// </summary>
    [HarmonyPatch(typeof(World), nameof(World.SpawnEntityInWorld))]
    public static class WorldSpawnEntityInWorldPatch
    {
        private static int _callCount;
        private static bool _firstCallLogged;

        [HarmonyPostfix]
        public static void Postfix(Entity _entity)
        {
            if (_entity == null) return;

            if (!_firstCallLogged)
            {
                _firstCallLogged = true;
                Log.Out($"[WakaTierCurve] SEW FIRST Postfix call: entity={_entity.GetType().Name}");
            }

            int inspectIdx = System.Threading.Interlocked.Increment(ref _callCount);
            bool verbose = inspectIdx <= 20;

            try
            {
                string name = "?";
                int classId = 0;
                try
                {
                    classId = _entity.entityClass;
                    if (EntityClass.list.TryGetValue(classId, out var ec) && ec != null)
                        name = ec.entityClassName;
                }
                catch { /* shrug */ }

                bool isBloodfall = name != null && (name.StartsWith("Brutal") || name.StartsWith("Alpha")
                    || name.StartsWith("Prime") || name.StartsWith("Apex") || name.StartsWith("Torment")
                    || name.StartsWith("Nightmare") || name.StartsWith("Hellborn") || name.StartsWith("Overlord")
                    || name.StartsWith("Demigod") || name.StartsWith("Bloodlord"));
                bool log = verbose || isBloodfall;
                if (!log) return;

                EnumSpawnerSource source = EnumSpawnerSource.Unknown;
                try { source = _entity.GetSpawnerSource(); } catch { }

                var pos = _entity.GetPosition();

                Log.Out($"[WakaTierCurve] SEW#{inspectIdx}: name='{name}' (id={classId}), spawnerSource={source}, type={_entity.GetType().Name}, pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}), entityId={_entity.entityId}{(isBloodfall ? " <<<BLOODFALL>>>" : "")}");
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaTierCurve] SEW Postfix failed: {e.Message}");
            }
        }
    }
}

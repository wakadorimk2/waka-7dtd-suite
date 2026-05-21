using System;
using System.Reflection;
using HarmonyLib;

namespace WakaTierCurve
{
    public class WakaTierCurveInit : IModApi
    {
        private static bool _inited;

        public void InitMod(Mod _modInstance)
        {
            if (_inited) return;
            _inited = true;

            Log.Out("[WakaTierCurve] InitMod start (Phase 4: single chokepoint at EntityFactory.CreateEntity)");

            try
            {
                var harmony = new Harmony("wakadori.wakatiercurve");

                // Single attribute-decorated patch on EntityFactory.CreateEntity(EntityCreationData).
                // Every entity creation in 7DTD funnels here — spawn paths (biome wandering,
                // POI sleeper, blood moon, quest, WalkerSim, MinEvent) all converge on this method,
                // AND chunk restore from region save files goes through it too. No upstream hooks
                // needed, no path-specific bypasses possible.
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Log.Out("[WakaTierCurve] Harmony patches applied.");

                Log.Out("[WakaTierCurve] TierMapper will lazy-init on first spawn.");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaTierCurve] InitMod failed: {e}");
            }
        }
    }
}

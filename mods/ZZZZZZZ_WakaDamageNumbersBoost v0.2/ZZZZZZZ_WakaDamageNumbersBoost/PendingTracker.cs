using HarmonyLib;
using System;
using System.Reflection;

namespace WakaDamageNumbersBoost
{
    // Hooks DamageController.CreateFloatingNumberFromClientData(PendingDamageNumber, bool).
    // While the original method is running, we publish the pending entry's flags +
    // damage value to ThreadStatic fields so any nested call into SetupTextMeshPro
    // can read them without having to retrofit the entire call chain.
    //
    // CreateFloatingNumberFromClientData is the choke point: it dispatches to all
    // Create*DamageNumber overloads (Floating / Shotgun / Bow=Zooming / Bleed=Dripping
    // / Fire=Wispy / Robo=Zapping / Explosive). Each of those calls SetupTextMeshPro
    // synchronously, so the pending data is live on this thread for that duration.
    [HarmonyPatch]
    public static class PendingTracker
    {
        [ThreadStatic] private static object _currentPending;
        [ThreadStatic] public static bool IsCritical;
        [ThreadStatic] public static bool IsHeadshot;
        [ThreadStatic] public static bool IsFatal;
        [ThreadStatic] public static int DamageDealt;

        public static bool HasActivePending => _currentPending != null;

        public static bool Prepare()
        {
            if (!TypeCache.Ready) TypeCache.Init();
            return TypeCache.Ready && TypeCache.CreateFloatingNumberFromClientDataMethod != null;
        }

        public static MethodBase TargetMethod()
        {
            return TypeCache.CreateFloatingNumberFromClientDataMethod;
        }

        public static void Prefix(object[] __args)
        {
            try
            {
                if (__args == null || __args.Length == 0) return;
                var pending = __args[0];
                if (pending == null) return;

                _currentPending = pending;
                IsCritical = (bool)TypeCache.IsCriticalField.GetValue(pending);
                IsHeadshot = (bool)TypeCache.IsHeadshotField.GetValue(pending);
                IsFatal = (bool)TypeCache.IsFatalField.GetValue(pending);
                if (TypeCache.DamageDealtField != null)
                {
                    DamageDealt = (int)TypeCache.DamageDealtField.GetValue(pending);
                }
                else
                {
                    DamageDealt = 0;
                }
            }
            catch (Exception e)
            {
                _currentPending = null;
                IsCritical = false;
                IsHeadshot = false;
                IsFatal = false;
                DamageDealt = 0;
                Log.Warning("[WakaDamageNumbersBoost] PendingTracker.Prefix: " + e);
            }
        }

        public static void Postfix()
        {
            _currentPending = null;
            IsCritical = false;
            IsHeadshot = false;
            IsFatal = false;
            DamageDealt = 0;
        }
    }
}

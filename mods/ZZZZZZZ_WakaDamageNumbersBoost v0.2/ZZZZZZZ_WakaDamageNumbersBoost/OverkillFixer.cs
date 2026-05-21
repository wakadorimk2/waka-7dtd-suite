using HarmonyLib;
using System;
using System.Reflection;

namespace WakaDamageNumbersBoost
{
    // === Overkill fix ===
    // Damage Numbers' DamageController.ProcessDamageEvent clips the displayed damage
    // to `healthBefore` when isFatal:
    //
    //   int num = finalDamage;
    //   if (isFatal) { num = healthBefore; ... overkill = Max(0, num3 - healthBefore); }
    //   QueueFloatingNumber(damageSource, num, ...);
    //
    // The raw damage (uncappedFatalDamage / initialStrength) is locally available,
    // and the mod even computes `overkill` for downstream handlers - but the floaty
    // itself is QueueFloatingNumber'd with the clipped value. That's the "1-shotted
    // a 100 HP zombie with a 500 damage shot, floaty says 100" problem.
    //
    // We capture the raw display value (healthBefore + overkill) in a ThreadStatic
    // during ProcessDamageEvent, then overwrite `damageDealt` via ref param when
    // QueueFloatingNumber runs inside the same call frame.
    public static class OverkillFixer
    {
        [ThreadStatic] private static int _pendingDisplayDamage;
        [ThreadStatic] private static bool _hasOverride;

        public static bool TryGetOverride(out int value)
        {
            if (_hasOverride)
            {
                value = _pendingDisplayDamage;
                return true;
            }
            value = 0;
            return false;
        }

        public static void Stash(int displayDamage)
        {
            _pendingDisplayDamage = displayDamage;
            _hasOverride = true;
        }

        public static void Clear()
        {
            _pendingDisplayDamage = 0;
            _hasOverride = false;
        }
    }

    [HarmonyPatch]
    public static class ProcessDamageEventTracker
    {
        public static bool Prepare()
        {
            if (!TypeCache.Ready) TypeCache.Init();
            return TypeCache.Ready && TypeCache.ProcessDamageEventMethod != null;
        }

        public static MethodBase TargetMethod()
        {
            return TypeCache.ProcessDamageEventMethod;
        }

        public static void Prefix(object[] __args)
        {
            try
            {
                OverkillFixer.Clear();
                if (__args == null || __args.Length < 9) return;

                // Args order from ilspy:
                //   0: DamageSource damageSource
                //   1: int initialStrength
                //   2: int finalDamage
                //   3: bool isFatal
                //   4: bool isCritical
                //   5: int healthBefore
                //   6: int uncappedFatalDamage
                //   7: bool isDismember
                //   8: EntityAlive victim
                int initialStrength = (int)__args[1];
                int finalDamage = (int)__args[2];
                bool isFatal = (bool)__args[3];
                int healthBefore = (int)__args[5];
                int uncappedFatalDamage = (int)__args[6];

                if (!isFatal) return;

                // Replicate the mod's own overkill calculation so we display
                // exactly what its internal handlers see, just without the clip.
                int num3;
                if (uncappedFatalDamage > healthBefore)
                {
                    num3 = uncappedFatalDamage;
                }
                else
                {
                    int mitigated = Math.Max(0, initialStrength - finalDamage);
                    num3 = initialStrength - mitigated;
                }
                int overkill = Math.Max(0, num3 - healthBefore);
                int display = healthBefore + overkill;
                if (display < 1) display = 1;

                OverkillFixer.Stash(display);
            }
            catch (Exception e)
            {
                OverkillFixer.Clear();
                Log.Warning("[WakaDamageNumbersBoost] ProcessDamageEventTracker.Prefix: " + e);
            }
        }

        public static void Postfix()
        {
            OverkillFixer.Clear();
        }
    }

    [HarmonyPatch]
    public static class QueueFloatingNumberOverride
    {
        public static bool Prepare()
        {
            if (!TypeCache.Ready) TypeCache.Init();
            return TypeCache.Ready && TypeCache.QueueFloatingNumberMethod != null;
        }

        public static MethodBase TargetMethod()
        {
            return TypeCache.QueueFloatingNumberMethod;
        }

        // ref int damageDealt: Harmony will bind by parameter name.
        public static void Prefix(ref int damageDealt)
        {
            try
            {
                if (OverkillFixer.TryGetOverride(out int real))
                {
                    damageDealt = real;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[WakaDamageNumbersBoost] QueueFloatingNumberOverride.Prefix: " + e);
            }
        }
    }
}

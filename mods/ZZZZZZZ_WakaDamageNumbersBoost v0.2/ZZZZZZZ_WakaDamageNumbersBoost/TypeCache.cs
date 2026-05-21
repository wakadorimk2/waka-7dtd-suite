using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace WakaDamageNumbersBoost
{
    // Discovers the Damage Numbers (1.9) assembly at runtime and caches the
    // reflective handles we need. DamageController is sealed and PendingDamageNumber
    // is a private nested type, so we can't reference them at compile time without
    // hard-binding to that mod. Lookup is best-effort: if the mod isn't loaded,
    // Ready stays false and every other patch self-disables via Prepare().
    public static class TypeCache
    {
        public static Assembly DnAssembly;

        public static Type DamageControllerType;
        public static Type PendingDamageNumberType;

        public static Type FloatingDamageNumberType;
        public static Type ZappingDamageNumberType;
        public static Type ZoomingDamageNumberType;
        public static Type ShotgunDamageNumberType;
        public static Type ExplosiveDamageNumberType;
        public static Type WispyDamageNumberType;
        public static Type DrippingDamageNumberType;

        public static FieldInfo IsCriticalField;
        public static FieldInfo IsHeadshotField;
        public static FieldInfo IsFatalField;
        public static FieldInfo DamageDealtField;

        public static FieldInfo FloatingFloatSpeedField;
        public static FieldInfo FloatingLifetimeField;

        public static MethodInfo CreateFloatingNumberFromClientDataMethod;
        public static MethodInfo SetupTextMeshProMethod;
        public static MethodInfo ProcessDamageEventMethod;
        public static MethodInfo QueueFloatingNumberMethod;

        public static bool Ready;
        public static bool Tried;

        public static void Init()
        {
            if (Tried) return;
            Tried = true;

            try
            {
                DnAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a =>
                    {
                        var n = a.GetName().Name;
                        return n == "DamageNumbersMod" || n == "HealthBarMod";
                    });

                if (DnAssembly == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] Damage Numbers assembly not loaded, patches will be inert.");
                    return;
                }

                DamageControllerType = DnAssembly.GetType("DamageController");
                FloatingDamageNumberType = DnAssembly.GetType("FloatingDamageNumber");
                ZappingDamageNumberType = DnAssembly.GetType("ZappingDamageNumber");
                ZoomingDamageNumberType = DnAssembly.GetType("ZoomingDamageNumber");
                ShotgunDamageNumberType = DnAssembly.GetType("ShotgunDamageNumber");
                ExplosiveDamageNumberType = DnAssembly.GetType("ExplosiveDamageNumber");
                WispyDamageNumberType = DnAssembly.GetType("WispyDamageNumber");
                DrippingDamageNumberType = DnAssembly.GetType("DrippingDamageNumber");

                if (DamageControllerType == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] DamageController type not found.");
                    return;
                }

                PendingDamageNumberType = DamageControllerType.GetNestedType(
                    "PendingDamageNumber",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

                if (PendingDamageNumberType == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] PendingDamageNumber nested type not found.");
                    return;
                }

                IsCriticalField = AccessTools.Field(PendingDamageNumberType, "IsCritical");
                IsHeadshotField = AccessTools.Field(PendingDamageNumberType, "IsHeadshot");
                IsFatalField = AccessTools.Field(PendingDamageNumberType, "IsFatal");
                DamageDealtField = AccessTools.Field(PendingDamageNumberType, "DamageDealt");

                if (FloatingDamageNumberType != null)
                {
                    FloatingFloatSpeedField = AccessTools.Field(FloatingDamageNumberType, "floatSpeed");
                    FloatingLifetimeField = AccessTools.Field(FloatingDamageNumberType, "lifetime");
                }

                CreateFloatingNumberFromClientDataMethod = AccessTools.Method(
                    DamageControllerType, "CreateFloatingNumberFromClientData",
                    new[] { PendingDamageNumberType, typeof(bool) });

                SetupTextMeshProMethod = AccessTools.Method(
                    DamageControllerType, "SetupTextMeshPro",
                    new[] { typeof(GameObject), typeof(string), typeof(float), typeof(Color) });

                // ProcessDamageEvent: signature pulled from ilspy dump:
                //   (DamageSource, int initialStrength, int finalDamage, bool isFatal, bool isCritical,
                //    int healthBefore, int uncappedFatalDamage, bool isDismember, EntityAlive victim)
                // We grab by name only since DamageSource type token is in the same assembly and
                // ilspy-correct param matching is fragile; AccessTools.Method will resolve overloads if any.
                ProcessDamageEventMethod = AccessTools.Method(DamageControllerType, "ProcessDamageEvent");

                // QueueFloatingNumber: (DamageSource, int damageDealt, bool isCritical, bool isFatal, EntityAlive victim)
                QueueFloatingNumberMethod = AccessTools.Method(DamageControllerType, "QueueFloatingNumber");

                if (IsCriticalField == null || IsHeadshotField == null || IsFatalField == null
                    || CreateFloatingNumberFromClientDataMethod == null
                    || SetupTextMeshProMethod == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] One or more required reflective handles not found.");
                    return;
                }

                if (DamageDealtField == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] PendingDamageNumber.DamageDealt not found; tier/format/overkill features will be inert.");
                }
                if (ProcessDamageEventMethod == null || QueueFloatingNumberMethod == null)
                {
                    Log.Warning("[WakaDamageNumbersBoost] ProcessDamageEvent or QueueFloatingNumber not found; overkill fix will be inert.");
                }

                Ready = true;
                Log.Out("[WakaDamageNumbersBoost] TypeCache ready (assembly=" + DnAssembly.GetName().Name + ").");
            }
            catch (Exception e)
            {
                Log.Warning("[WakaDamageNumbersBoost] TypeCache.Init failed: " + e);
            }
        }
    }
}

using System;
using System.Linq;
using System.Reflection;

namespace WakaRamAffixCurve
{
    /// <summary>
    /// Resolves RAM (WeaponAffixesProject) internals via reflection. We need
    /// AffixSystem.CountModsToApply (the Postfix target) and AffixUtils.IsAffixMod
    /// (used inside the Postfix to count already-applied affixes).
    /// </summary>
    internal static class RamCurveBridge
    {
        public static bool Ready { get; private set; }
        public static MethodInfo CountModsToApplyMethod { get; private set; }
        static MethodInfo isAffixModMethod;

        public static void Initialize()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "WeaponAffixesProject");
                if (asm == null)
                {
                    Log.Out("[WakaRamAffixCurve] WeaponAffixesProject assembly not loaded");
                    return;
                }

                const BindingFlags bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                CountModsToApplyMethod = TryResolve(asm, "WeaponAffixesProject.AffixSystem",
                    "CountModsToApply", new[] { typeof(ItemValue), typeof(EntityPlayer) }, bf);

                isAffixModMethod = TryResolve(asm, "WeaponAffixesProject.AffixUtils",
                    "IsAffixMod", new[] { typeof(ItemClass) }, bf);

                Ready = CountModsToApplyMethod != null && isAffixModMethod != null;
                Log.Out($"[WakaRamAffixCurve] Bridge ready={Ready} (CountModsToApply={CountModsToApplyMethod != null}, IsAffixMod={isAffixModMethod != null})");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaRamAffixCurve] Bridge init failed: {e}");
            }
        }

        public static bool IsAffixMod(ItemClass itemClass)
        {
            if (itemClass == null || isAffixModMethod == null) return false;
            try
            {
                return (bool)isAffixModMethod.Invoke(null, new object[] { itemClass });
            }
            catch (Exception e)
            {
                Log.Out($"[WakaRamAffixCurve] IsAffixMod invoke failed: {e.Message}");
                return false;
            }
        }

        static MethodInfo TryResolve(Assembly asm, string typeFullName, string methodName, Type[] paramTypes, BindingFlags bf)
        {
            try
            {
                var t = asm.GetType(typeFullName);
                if (t == null)
                {
                    Log.Out($"[WakaRamAffixCurve] type not found: {typeFullName}");
                    return null;
                }
                var m = t.GetMethod(methodName, bf, null, paramTypes, null);
                if (m == null)
                {
                    Log.Out($"[WakaRamAffixCurve] method not found: {typeFullName}.{methodName}");
                    return null;
                }
                Log.Out($"[WakaRamAffixCurve] Resolved {typeFullName}.{methodName}");
                return m;
            }
            catch (Exception e)
            {
                Log.Out($"[WakaRamAffixCurve] Resolve failed for {typeFullName}.{methodName}: {e.Message}");
                return null;
            }
        }
    }
}

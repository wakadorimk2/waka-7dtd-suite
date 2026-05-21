using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WakaRamNullGuard
{
    /// <summary>
    /// Resolves RAM (WeaponAffixesProject) methods that need null guards via
    /// reflection so this mod has no compile-time dependency on RAM. Each
    /// guarded RAM method is held in a separate property; patches check the
    /// matching Ready flag in their Prepare().
    /// </summary>
    internal static class RamBridge
    {
        public static bool ChallengeReady { get; private set; }
        public static MethodInfo ChallengeGroupIsCompletedMethod { get; private set; }

        public static bool LootCheckReady { get; private set; }
        public static MethodInfo ApplyAffixToLootCheckMethod { get; private set; }

        public static bool ResurgenceReady { get; private set; }
        public static MethodInfo ResurgenceCheckMethod { get; private set; }

        public static bool BringItDownReady { get; private set; }
        public static MethodInfo BringItDownCheckMethod { get; private set; }

        public static bool PermadeathReady { get; private set; }
        public static MethodInfo PermadeathCheckMethod { get; private set; }

        public static bool GiantSlayerReady { get; private set; }
        public static MethodInfo GiantSlayerCheckMethod { get; private set; }

        public static bool BulletRecoveryReady { get; private set; }
        public static MethodInfo BulletRecoveryCheckMethod { get; private set; }

        public static bool AnyReady =>
            ChallengeReady || LootCheckReady || ResurgenceReady ||
            BringItDownReady || PermadeathReady || GiantSlayerReady ||
            BulletRecoveryReady;

        public static void Initialize()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "WeaponAffixesProject");
                if (asm == null)
                {
                    Log.Out("[WakaRamNullGuard] WeaponAffixesProject assembly not loaded");
                    return;
                }

                const BindingFlags bf = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                ChallengeGroupIsCompletedMethod = TryResolve(asm, "WeaponAffixesProject.AffixUtils",
                    "ChallengeGroupIsCompleted", new[] { typeof(EntityPlayer), typeof(string) }, bf);
                ChallengeReady = ChallengeGroupIsCompletedMethod != null;

                ApplyAffixToLootCheckMethod = TryResolve(asm, "WeaponAffixesProject.AffixSystem",
                    "ApplyAffixToLootCheck", new[] { typeof(List<ItemStack>), typeof(EntityPlayer) }, bf);
                LootCheckReady = ApplyAffixToLootCheckMethod != null;

                ResurgenceCheckMethod = TryResolve(asm, "WeaponAffixesProject.AffixResurgence",
                    "ResurgenceCheck", new[] { typeof(EntityPlayer), typeof(DamageSource) }, bf);
                ResurgenceReady = ResurgenceCheckMethod != null;

                BringItDownCheckMethod = TryResolve(asm, "WeaponAffixesProject.AffixBringItDown",
                    "BringItDownCheck", new[] { typeof(EntityEnemy), typeof(DamageSource) }, bf);
                BringItDownReady = BringItDownCheckMethod != null;

                PermadeathCheckMethod = TryResolve(asm, "WeaponAffixesProject.AffixPermadeath",
                    "PermadeathCheck", new[] { typeof(EntityEnemy), typeof(DamageSource) }, bf);
                PermadeathReady = PermadeathCheckMethod != null;

                GiantSlayerCheckMethod = TryResolve(asm, "WeaponAffixesProject.AffixGiantSlayer",
                    "GiantSlayerCheck", new[] { typeof(EntityEnemy), typeof(DamageSource) }, bf);
                GiantSlayerReady = GiantSlayerCheckMethod != null;

                BulletRecoveryCheckMethod = TryResolve(asm, "WeaponAffixesProject.Affixes.AffixBulletRecovery",
                    "BulletRecoveryCheck", new[] { typeof(ItemActionData) }, bf);
                BulletRecoveryReady = BulletRecoveryCheckMethod != null;

                Log.Out($"[WakaRamNullGuard] Bridge ready: challenge={ChallengeReady} loot={LootCheckReady} resurgence={ResurgenceReady} bringitdown={BringItDownReady} permadeath={PermadeathReady} giantslayer={GiantSlayerReady} bulletrecovery={BulletRecoveryReady}");
            }
            catch (Exception e)
            {
                Log.Error($"[WakaRamNullGuard] Bridge init failed: {e}");
            }
        }

        static MethodInfo TryResolve(Assembly asm, string typeFullName, string methodName, Type[] paramTypes, BindingFlags bf)
        {
            try
            {
                var t = asm.GetType(typeFullName);
                if (t == null)
                {
                    Log.Out($"[WakaRamNullGuard] type not found: {typeFullName}");
                    return null;
                }
                var m = t.GetMethod(methodName, bf, null, paramTypes, null);
                if (m == null)
                {
                    Log.Out($"[WakaRamNullGuard] method not found: {typeFullName}.{methodName}");
                    return null;
                }
                Log.Out($"[WakaRamNullGuard] Resolved {typeFullName}.{methodName}");
                return m;
            }
            catch (Exception e)
            {
                Log.Out($"[WakaRamNullGuard] Resolve failed for {typeFullName}.{methodName}: {e.Message}");
                return null;
            }
        }
    }
}

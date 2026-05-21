using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WakaTierCurve
{
    /// <summary>
    /// Discovers tier-able zombie entities by naming convention and maps them
    /// to (baseName, tier) pairs. baseName is the "stem" shared across tiers
    /// (e.g. "Boe" for zombieBoe / zombieBoeT2 / BrutalBoeTier6).
    /// </summary>
    internal static class TierMapper
    {
        // vanilla / EZS: zombieXxx (T1), zombieXxxT2..T5
        // Note: Xxx may itself contain "Feral" / "Radiated" suffixes (e.g. zombieBoeFeralT3)
        private static readonly Regex RxVanilla = new Regex(
            @"^zombie([A-Z][A-Za-z]+?)(T([2-5]))?$",
            RegexOptions.Compiled);

        // Bloodfall: <Prefix><Xxx>Tier<N>
        private static readonly Regex RxBloodfall = new Regex(
            @"^(Brutal|Alpha|Prime|Apex|Torment|Nightmare|Hellborn|Overlord|Demigod|Bloodlord)([A-Z][A-Za-z]+?)Tier(\d+)$",
            RegexOptions.Compiled);

        private static readonly Dictionary<string, int> BloodfallPrefixToTier = new Dictionary<string, int>
        {
            { "Brutal",    6 },
            { "Alpha",     7 },
            { "Prime",     8 },
            { "Apex",      9 },
            { "Torment",   10 },
            { "Nightmare", 11 },
            { "Hellborn",  12 },
            { "Overlord",  13 },
            { "Demigod",   14 },
            { "Bloodlord", 15 },
        };

        // baseName -> tier -> entityClassId
        private static Dictionary<string, Dictionary<int, int>> _byBase;
        // entityClassId -> (baseName, tier)
        private static Dictionary<int, (string Base, int Tier)> _byId;

        private static bool _initialized;
        private static readonly object _lock = new object();

        /// <summary>
        /// Lazily scans EntityClass.list and builds tier maps.
        /// Only baseNames that have at least one EZS T2-T5 variant are considered
        /// "tier-able"; this confines swaps to zombies that the player ecosystem
        /// already understands as tiered.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                Build();
                _initialized = true;
            }
        }

        private static void Build()
        {
            _byBase = new Dictionary<string, Dictionary<int, int>>();
            _byId = new Dictionary<int, (string Base, int Tier)>();

            // Step 1: discover tier-able baseNames.
            //   1a) zombieXxxT2..T5 (EZS-supplied vanilla tier variants)
            //   1b) <Prefix>XxxTier<N> (Bloodfall variants) — handles bases like Wight where
            //       EZS supplies WightFeral/WightRadiated T2-T5 but no plain Wight Tx, so
            //       step 1a misses "Wight" even though BrutalWightTier6 etc. exist.
            var tieredBases = new HashSet<string>();
            foreach (var kv in EntityClass.list.Dict)
            {
                var ec = kv.Value;
                if (ec == null) continue;
                var name = ec.entityClassName;
                if (string.IsNullOrEmpty(name)) continue;

                var mv = RxVanilla.Match(name);
                if (mv.Success && mv.Groups[2].Success)
                {
                    tieredBases.Add(mv.Groups[1].Value);
                    continue;
                }

                var mb = RxBloodfall.Match(name);
                if (mb.Success)
                {
                    tieredBases.Add(mb.Groups[2].Value);
                }
            }

            int total = 0;
            // Step 2: for each baseName, slot all known tiers we can find.
            foreach (var baseName in tieredBases)
            {
                var tierToId = new Dictionary<int, int>();

                // T1: zombie<Base>
                if (TryResolve("zombie" + baseName, out int t1Id))
                    tierToId[1] = t1Id;

                // T2-T5: zombie<Base>T<N>
                for (int t = 2; t <= 5; t++)
                {
                    if (TryResolve("zombie" + baseName + "T" + t, out int tid))
                        tierToId[t] = tid;
                }

                // T6-T15: <Prefix><Base>Tier<N>
                foreach (var kv in BloodfallPrefixToTier)
                {
                    var bfName = kv.Key + baseName + "Tier" + kv.Value;
                    if (TryResolve(bfName, out int bfId))
                        tierToId[kv.Value] = bfId;
                }

                if (tierToId.Count <= 1) continue; // not actually tiered

                _byBase[baseName] = tierToId;
                foreach (var pair in tierToId)
                {
                    _byId[pair.Value] = (baseName, pair.Key);
                    total++;
                }
            }

            Log.Out($"[WakaTierCurve] Discovered {_byBase.Count} tier-able baseNames, {total} entity entries total.");
        }

        private static bool TryResolve(string entityClassName, out int classId)
        {
            classId = 0;
            int hash = entityClassName.GetHashCode();
            if (!EntityClass.list.TryGetValue(hash, out var ec)) return false;
            if (ec == null) return false;
            // Hash collision safety: confirm the registered entity actually has this name.
            if (ec.entityClassName != entityClassName) return false;
            classId = hash;
            return true;
        }

        /// <summary>Returns true if the entity name maps to a known (base, tier) pair.</summary>
        public static bool TryGetByClassId(int classId, out string baseName, out int tier)
        {
            EnsureInitialized();
            if (_byId.TryGetValue(classId, out var pair))
            {
                baseName = pair.Base;
                tier = pair.Tier;
                return true;
            }
            baseName = null;
            tier = 0;
            return false;
        }

        /// <summary>Resolves (baseName, tier) back to an entity class id, if defined.</summary>
        public static bool TryGetClassId(string baseName, int tier, out int classId)
        {
            EnsureInitialized();
            classId = 0;
            if (_byBase.TryGetValue(baseName, out var tierToId)
                && tierToId.TryGetValue(tier, out classId))
            {
                return true;
            }
            return false;
        }

        /// <summary>Tiers actually defined for the given baseName (may be sparse).</summary>
        public static IReadOnlyDictionary<int, int> TiersFor(string baseName)
        {
            EnsureInitialized();
            return _byBase.TryGetValue(baseName, out var d) ? d : null;
        }
    }
}

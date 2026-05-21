using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace WakaSoundExpReverbTune
{
    // Tuning constants. Picked together with わかどりちゃん, option A (中平衡):
    //   rt60 cap 1.8s -> 0.7s
    //   small-room boost Lerp(1, 2.5) -> Lerp(1, 1.5) approximated by wetGain *= 0.71
    //   wet coefficient Lerp(0.3, 0.6) -> Lerp(0.20, 0.40) approximated by src.volume *= 0.667
    //   wood mid-band absorption ~0.07-0.10 -> ~0.20 (clamp-up only, never lowered)
    internal static class TuneConfig
    {
        public const float Rt60Cap = 0.7f;
        public const float WetGainScale = 0.71f;
        public const float SrcVolumeScale = 0.667f;

        // bands: 125, 250, 500, 1000, 2000, 4000, 8000 Hz
        public static readonly Dictionary<string, float[]> WoodAbsTargets =
            new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mwood",           new[] { 0.25f, 0.20f, 0.18f, 0.15f, 0.12f, 0.10f, 0.10f } },
                { "Mwood_regular",   new[] { 0.25f, 0.20f, 0.18f, 0.15f, 0.12f, 0.10f, 0.10f } },
                { "Mwood_shapes",    new[] { 0.25f, 0.20f, 0.18f, 0.15f, 0.12f, 0.10f, 0.10f } },
                { "MwoodReinforced", new[] { 0.22f, 0.18f, 0.16f, 0.13f, 0.11f, 0.09f, 0.10f } },
                { "MwoodMetal",      new[] { 0.18f, 0.14f, 0.12f, 0.10f, 0.09f, 0.09f, 0.10f } },
            };
    }

    internal static class SoundExpTypes
    {
        private static Assembly _asm;
        private static Type _reverbProcessor;
        private static Type _reverbPool;
        private static Type _voxelField;
        private static bool _scanned;

        public static Assembly Asm
        {
            get { Scan(); return _asm; }
        }
        public static Type ReverbProcessor
        {
            get { Scan(); return _reverbProcessor; }
        }
        public static Type ReverbPool
        {
            get { Scan(); return _reverbPool; }
        }
        public static Type VoxelField
        {
            get { Scan(); return _voxelField; }
        }

        private static void Scan()
        {
            if (_scanned) return;
            _scanned = true;
            try
            {
                _asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SoundExpMod");
                if (_asm == null)
                {
                    Log.Warning("[WakaSoundExpReverbTune] SoundExpMod assembly not found, patches will be inert.");
                    return;
                }
                _reverbProcessor = _asm.GetType("SoundExp.ReverbProcessor");
                _reverbPool = _asm.GetType("SoundExp.ReverbPool");
                _voxelField = _asm.GetType("SoundExp.VoxelField");
                if (_reverbProcessor == null) Log.Warning("[WakaSoundExpReverbTune] SoundExp.ReverbProcessor not found.");
                if (_reverbPool == null) Log.Warning("[WakaSoundExpReverbTune] SoundExp.ReverbPool not found.");
                if (_voxelField == null) Log.Warning("[WakaSoundExpReverbTune] SoundExp.VoxelField not found.");
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSoundExpReverbTune] Type lookup failed: " + e);
            }
        }
    }

    // Patch 1: clamp rt60 ceiling and scale wetGain so the small-room boost
    // (Lerp(1, 2.5, ...) inside Process) ends up closer to Lerp(1, 1.5, ...).
    [HarmonyPatch]
    public static class ReverbProcessor_Configure_Patch
    {
        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            var t = SoundExpTypes.ReverbProcessor;
            if (t == null) return null;
            var m = t.GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);
            if (m == null)
                Log.Warning("[WakaSoundExpReverbTune] ReverbProcessor.Configure not found.");
            return m;
        }

        public static void Prefix(ref float rt60, ref float wetGain)
        {
            if (rt60 > TuneConfig.Rt60Cap) rt60 = TuneConfig.Rt60Cap;
            wetGain *= TuneConfig.WetGainScale;
        }
    }

    // Patch 2: scale the AudioSource volume of the just-assigned reverb pool entry,
    // approximating a reduction of the wet send coefficient inside PlayReverb.
    [HarmonyPatch]
    public static class ReverbPool_PlayReverb_Patch
    {
        private static FieldInfo _poolField;
        private static FieldInfo _entrySrcField;
        private static FieldInfo _entryOriginalField;
        private static bool _fieldsInited;

        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            var t = SoundExpTypes.ReverbPool;
            if (t == null) return null;
            var m = t.GetMethod("PlayReverb", BindingFlags.Public | BindingFlags.Static);
            if (m == null)
                Log.Warning("[WakaSoundExpReverbTune] ReverbPool.PlayReverb not found.");
            return m;
        }

        private static void EnsureFields()
        {
            if (_fieldsInited) return;
            _fieldsInited = true;
            try
            {
                var poolType = SoundExpTypes.ReverbPool;
                if (poolType == null) return;
                _poolField = poolType.GetField("pool", BindingFlags.NonPublic | BindingFlags.Static);
                if (_poolField == null) { Log.Warning("[WakaSoundExpReverbTune] ReverbPool.pool not found."); return; }
                var entryType = _poolField.FieldType.GetElementType();
                if (entryType == null) { Log.Warning("[WakaSoundExpReverbTune] PoolEntry element type not found."); return; }
                _entrySrcField = entryType.GetField("src", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _entryOriginalField = entryType.GetField("original", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (_entrySrcField == null) Log.Warning("[WakaSoundExpReverbTune] PoolEntry.src not found.");
                if (_entryOriginalField == null) Log.Warning("[WakaSoundExpReverbTune] PoolEntry.original not found.");
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSoundExpReverbTune] EnsureFields failed: " + e);
            }
        }

        public static void Postfix(AudioSource original)
        {
            try
            {
                if (original == null) return;
                EnsureFields();
                if (_poolField == null || _entrySrcField == null || _entryOriginalField == null) return;
                var pool = _poolField.GetValue(null) as Array;
                if (pool == null) return;
                for (int i = 0; i < pool.Length; i++)
                {
                    var entry = pool.GetValue(i);
                    if (entry == null) continue;
                    var origVal = _entryOriginalField.GetValue(entry) as AudioSource;
                    if (!ReferenceEquals(origVal, original)) continue;
                    var src = _entrySrcField.GetValue(entry) as AudioSource;
                    if (src == null) continue;
                    src.volume *= TuneConfig.SrcVolumeScale;
                    break;
                }
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSoundExpReverbTune] PlayReverb postfix failed: " + e);
            }
        }
    }

    // Patch 3: after SoundExp loads SoundMaterials.xml into its private dict,
    // clamp-up wood materials' band absorptions toward more realistic values
    // and recompute reflectivity / hfReflectivity. Never lowers an existing value.
    [HarmonyPatch]
    public static class VoxelField_LoadSoundMaterials_Patch
    {
        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            var t = SoundExpTypes.VoxelField;
            if (t == null) return null;
            var m = t.GetMethod("LoadSoundMaterials", BindingFlags.Public | BindingFlags.Static);
            if (m == null)
                Log.Warning("[WakaSoundExpReverbTune] VoxelField.LoadSoundMaterials not found.");
            return m;
        }

        public static void Postfix()
        {
            try
            {
                var voxelType = SoundExpTypes.VoxelField;
                if (voxelType == null) return;
                var dictField = voxelType.GetField("materialAcoustics", BindingFlags.NonPublic | BindingFlags.Static);
                if (dictField == null)
                {
                    Log.Warning("[WakaSoundExpReverbTune] materialAcoustics field not found.");
                    return;
                }
                var dict = dictField.GetValue(null) as IDictionary;
                if (dict == null)
                {
                    Log.Warning("[WakaSoundExpReverbTune] materialAcoustics dict null or non-IDictionary.");
                    return;
                }

                int patched = 0;
                foreach (var kv in TuneConfig.WoodAbsTargets)
                {
                    string id = kv.Key;
                    float[] targets = kv.Value;
                    if (!dict.Contains(id)) continue;
                    object boxed = dict[id];
                    if (boxed == null) continue;
                    var t = boxed.GetType();

                    string[] absNames = { "abs125", "abs250", "abs500", "abs1000", "abs2000", "abs4000", "abs8000" };
                    var absFields = new FieldInfo[absNames.Length];
                    bool ok = true;
                    for (int i = 0; i < absNames.Length; i++)
                    {
                        absFields[i] = t.GetField(absNames[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (absFields[i] == null) { ok = false; break; }
                    }
                    if (!ok)
                    {
                        Log.Warning("[WakaSoundExpReverbTune] AcousticProps abs fields incomplete; aborting.");
                        return;
                    }

                    float[] applied = new float[absNames.Length];
                    for (int i = 0; i < absNames.Length; i++)
                    {
                        float current = (float)absFields[i].GetValue(boxed);
                        float final = Mathf.Max(current, targets[i]);
                        applied[i] = final;
                        absFields[i].SetValue(boxed, final);
                    }

                    float meanAll = (applied[0] + applied[1] + applied[2] + applied[3] + applied[4] + applied[5] + applied[6]) / 7f;
                    float meanHf = (applied[4] + applied[5] + applied[6]) / 3f;

                    var reflField = t.GetField("reflectivity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    var hfReflField = t.GetField("hfReflectivity", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (reflField != null) reflField.SetValue(boxed, 1f - meanAll);
                    if (hfReflField != null) hfReflField.SetValue(boxed, 1f - meanHf);

                    dict[id] = boxed;
                    patched++;
                }

                Log.Out($"[WakaSoundExpReverbTune] Wood absorption clamp-up applied to {patched} material(s).");
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSoundExpReverbTune] LoadSoundMaterials postfix failed: " + e);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace WakaPet
{
    /// <summary>
    /// v0.8 voice playback. mod の Resources/voice/*.wav (16-bit PCM) をプリロードし、
    /// trigger 側から key で再生する。3D AudioSource を entity GameObject に attach。
    /// </summary>
    public static class WakaPetVoice
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static bool initialized;

        public static int ClipCount => clips.Count;

        public static void Init(string modPath)
        {
            if (initialized) return;
            initialized = true;

            string voiceDir = Path.Combine(modPath, "Resources", "voice");
            if (!Directory.Exists(voiceDir))
            {
                Log.Warning($"[WakaPet/Voice] voice dir not found: {voiceDir}");
                return;
            }

            int loaded = 0, failed = 0;
            foreach (var wavPath in Directory.GetFiles(voiceDir, "*.wav"))
            {
                string key = Path.GetFileNameWithoutExtension(wavPath);
                try
                {
                    clips[key] = LoadWavAsClip(wavPath, key);
                    loaded++;
                }
                catch (Exception e)
                {
                    Log.Warning($"[WakaPet/Voice] load FAIL {key}: {e.Message}");
                    failed++;
                }
            }
            Log.Out($"[WakaPet/Voice] loaded {loaded} clips (failed={failed}) from {voiceDir}");
        }

        public static bool Has(string key) => clips.ContainsKey(key);

        public static void Play(string key, GameObject host, float volume = 2f)
        {
            if (host == null) return;
            if (!clips.TryGetValue(key, out var clip) || clip == null)
            {
                Log.Warning($"[WakaPet/Voice] missing clip key: {key}");
                return;
            }

            var src = host.GetComponent<AudioSource>();
            if (src == null)
            {
                src = host.AddComponent<AudioSource>();
                src.spatialBlend = 1f;
                src.minDistance = 2f;
                src.maxDistance = 30f;
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.dopplerLevel = 0f;
                src.priority = 128;
                src.playOnAwake = false;
                src.bypassEffects = true;
            }
            src.PlayOneShot(clip, volume);
            Log.Out($"[WakaPet/Voice] play {key} on entity GO={host.name}");
        }

        public static void PlayRandom(string[] keys, GameObject host, float volume = 2f)
        {
            if (host == null || keys == null || keys.Length == 0) return;
            string k = keys[UnityEngine.Random.Range(0, keys.Length)];
            Play(k, host, volume);
        }

        // 16-bit PCM WAV を AudioClip に変換。pedalboard 出力 (44.1kHz mono/stereo) 想定。
        static AudioClip LoadWavAsClip(string path, string name)
        {
            byte[] data = File.ReadAllBytes(path);
            if (data.Length < 44) throw new Exception("file too small");
            if (Encoding.ASCII.GetString(data, 0, 4) != "RIFF") throw new Exception("not RIFF");
            if (Encoding.ASCII.GetString(data, 8, 4) != "WAVE") throw new Exception("not WAVE");

            int fmtOff = FindChunk(data, "fmt ");
            if (fmtOff < 0) throw new Exception("fmt chunk missing");
            int audioFormat   = BitConverter.ToInt16(data, fmtOff + 8);
            int channels      = BitConverter.ToInt16(data, fmtOff + 10);
            int sampleRate    = BitConverter.ToInt32(data, fmtOff + 12);
            int bitsPerSample = BitConverter.ToInt16(data, fmtOff + 22);
            if (audioFormat != 1) throw new Exception($"unsupported audioFormat={audioFormat}");
            if (bitsPerSample != 16) throw new Exception($"unsupported bits={bitsPerSample}");

            int dataOff = FindChunk(data, "data");
            if (dataOff < 0) throw new Exception("data chunk missing");
            int dataSize = BitConverter.ToInt32(data, dataOff + 4);
            int pcmStart = dataOff + 8;

            int totalSamples = dataSize / 2;
            int sampleCountPerChannel = totalSamples / channels;
            float[] samples = new float[totalSamples];
            for (int i = 0; i < totalSamples; i++)
            {
                short s = BitConverter.ToInt16(data, pcmStart + i * 2);
                samples[i] = s / 32768f;
            }

            var clip = AudioClip.Create(name, sampleCountPerChannel, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static int FindChunk(byte[] data, string id)
        {
            int offset = 12;
            while (offset + 8 <= data.Length)
            {
                if (Encoding.ASCII.GetString(data, offset, 4) == id) return offset;
                int chunkSize = BitConverter.ToInt32(data, offset + 4);
                offset += 8 + chunkSize;
            }
            return -1;
        }
    }
}

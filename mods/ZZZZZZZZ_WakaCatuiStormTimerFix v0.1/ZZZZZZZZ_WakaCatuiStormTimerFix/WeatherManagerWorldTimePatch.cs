using HarmonyLib;
using System;

namespace WakaCatuiStormTimerFix
{
    [HarmonyPatch(typeof(WeatherManager), nameof(WeatherManager.SetWorldTime))]
    public static class WeatherManagerWorldTimePatch
    {
        private const ulong LargeForwardJumpWorldTime = 250UL;

        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(out int __state)
        {
            __state = WeatherManager.worldTime;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ulong _time, int __state)
        {
            try
            {
                int currentWorldTime = WeatherManager.worldTime;
                if (currentWorldTime <= __state)
                {
                    return;
                }

                ulong delta = (ulong)(currentWorldTime - __state);
                if (delta < LargeForwardJumpWorldTime)
                {
                    return;
                }

                NormalizeExpiredStorms(currentWorldTime, delta);
            }
            catch (Exception ex)
            {
                Log.Warning("[WakaCatuiStormTimerFix] Failed to normalize storm timers after world-time jump: " + ex);
            }
        }

        private static void NormalizeExpiredStorms(int currentWorldTime, ulong delta)
        {
            WeatherManager manager = WeatherManager.Instance;
            if (!manager || manager.biomeWeather == null)
            {
                return;
            }

            int normalizedCount = 0;
            for (int i = 0; i < manager.biomeWeather.Count; i++)
            {
                WeatherManager.BiomeWeather biomeWeather = manager.biomeWeather[i];
                if (biomeWeather == null || biomeWeather.stormWorldTime <= 0 || biomeWeather.stormDuration <= 0)
                {
                    continue;
                }

                long stormEndWorldTime = (long)biomeWeather.stormWorldTime + biomeWeather.stormDuration;
                if (stormEndWorldTime > currentWorldTime)
                {
                    continue;
                }

                biomeWeather.stormState = 0;
                biomeWeather.stormWorldTime = 0;
                biomeWeather.stormDuration = 0;
                biomeWeather.nextRandWorldTime = 0;
                biomeWeather.remainingSeconds = 0;
                normalizedCount++;
            }

            if (normalizedCount <= 0)
            {
                return;
            }

            manager.TriggerUpdate();
            Log.Out("[WakaCatuiStormTimerFix] Normalized {0} expired storm reservation(s) after world-time jump of {1}.", normalizedCount, delta);
        }
    }
}

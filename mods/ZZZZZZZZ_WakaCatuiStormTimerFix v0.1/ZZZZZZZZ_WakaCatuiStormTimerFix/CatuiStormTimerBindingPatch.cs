using HarmonyLib;
using UnityEngine;

namespace WakaCatuiStormTimerFix
{
    [HarmonyPatch(typeof(XUiC_CompassWindow), "GetBindingValueInternal")]
    public static class CatuiStormTimerBindingPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static bool Prefix(ref string value, string bindingName, ref bool __result, XUiC_CompassWindow __instance)
        {
            if (!IsStormBinding(bindingName))
            {
                return true;
            }

            try
            {
                WriteClampedStormBinding(ref value, bindingName, __instance);
            }
            catch
            {
                value = bindingName == "CATUI_stormFill" ? "0.000" : "0";
            }

            __result = true;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref string value, string bindingName, ref bool __result, XUiC_CompassWindow __instance)
        {
            if (!IsStormBinding(bindingName))
            {
                return;
            }

            try
            {
                WriteClampedStormBinding(ref value, bindingName, __instance);
                __result = true;
            }
            catch
            {
                value = bindingName == "CATUI_stormFill" ? "0.000" : "0";
                __result = true;
            }
        }

        private static bool IsStormBinding(string bindingName)
        {
            switch (bindingName)
            {
                case "CATUI_stormDurationTimeReal":
                case "CATUI_stormDurationTime":
                case "CATUI_stormRemainingTime":
                case "CATUI_stormStartWorldTime":
                case "CATUI_stormEndWorldTime":
                case "CATUI_stormFill":
                    return true;
                default:
                    return false;
            }
        }

        private static void WriteClampedStormBinding(ref string value, string bindingName, XUiC_CompassWindow compass)
        {
            value = bindingName == "CATUI_stormFill" ? "0.000" : "0";

            EntityPlayerLocal localPlayer = compass?.localPlayer;
            if ((Object)localPlayer == null)
            {
                return;
            }

            BiomeDefinition biome = localPlayer.biomeStandingOn;
            if (biome == null)
            {
                return;
            }

            WeatherManager.BiomeWeather biomeWeather = WeatherManager.Instance?.FindBiomeWeather(biome.m_BiomeType);
            int stormLevel = biomeWeather?.stormState ?? 0;
            if (biomeWeather == null || stormLevel <= 0)
            {
                return;
            }

            int stormEndWorldTime = biomeWeather.stormWorldTime + biomeWeather.stormDuration;
            int remainingWorldTime = Mathf.Max(0, stormEndWorldTime - WeatherManager.worldTime);
            switch (bindingName)
            {
                case "CATUI_stormDurationTimeReal":
                    int timeOfDayIncPerSec = Mathf.Max(1, GameStats.GetInt(EnumGameStats.TimeOfDayIncPerSec));
                    int remainingRealSeconds = remainingWorldTime / timeOfDayIncPerSec;
                    value = remainingRealSeconds <= 0 ? "" : XUiM_PlayerBuffs.ConvertToTimeString(remainingRealSeconds);
                    return;
                case "CATUI_stormDurationTime":
                case "CATUI_stormRemainingTime":
                    value = remainingWorldTime.ToString();
                    return;
                case "CATUI_stormStartWorldTime":
                    value = biomeWeather.stormWorldTime.ToString();
                    return;
                case "CATUI_stormEndWorldTime":
                    value = stormEndWorldTime.ToString();
                    return;
                case "CATUI_stormFill":
                    if (biomeWeather.stormDuration <= 0)
                    {
                        value = "0.000";
                        return;
                    }
                    float fill = Mathf.Clamp01((float)remainingWorldTime / biomeWeather.stormDuration);
                    value = fill.ToString("F3");
                    return;
            }
        }
    }
}

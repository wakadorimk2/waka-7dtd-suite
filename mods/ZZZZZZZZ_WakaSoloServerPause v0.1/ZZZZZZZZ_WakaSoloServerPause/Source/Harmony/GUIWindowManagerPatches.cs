using HarmonyLib;

namespace WakaSoloServerPause
{
    [HarmonyPatch(typeof(GUIWindowManager), nameof(GUIWindowManager.Open), new[] { typeof(GUIWindow), typeof(bool), typeof(bool), typeof(bool) })]
    public static class GUIWindowManager_Open_EscPausePatch
    {
        public static void Postfix(GUIWindowManager __instance, GUIWindow _w)
        {
            ClientEscMenuPauseBridge.CheckInGameMenuState(__instance, $"open:{_w?.Id ?? "unknown"}");
        }
    }

    [HarmonyPatch(typeof(GUIWindowManager), nameof(GUIWindowManager.Close), new[] { typeof(GUIWindow), typeof(bool) })]
    public static class GUIWindowManager_Close_EscPausePatch
    {
        public static void Postfix(GUIWindowManager __instance, GUIWindow _w)
        {
            ClientEscMenuPauseBridge.CheckInGameMenuState(__instance, $"close:{_w?.Id ?? "unknown"}");
        }
    }
}

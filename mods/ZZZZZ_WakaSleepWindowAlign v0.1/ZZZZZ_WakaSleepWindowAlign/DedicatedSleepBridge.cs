using HarmonyLib;
using System;
using System.Reflection;

namespace WakaSleepWindowAlign
{
    [HarmonyPatch]
    public static class TFDSleepDialog_DedicatedBridge
    {
        private const string CommandPrefix = "waka_sleep_skip ";

        public static bool Prepare()
        {
            return TargetMethod() != null;
        }

        public static MethodBase TargetMethod()
        {
            var asm = SleepAssembly();
            var dialogType = asm?.GetType("XUiC_TFDSleepDialog");
            return dialogType?.GetMethod("Confirm", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static void Prefix(object __instance)
        {
            var cm = SingletonMonoBehaviour<ConnectionManager>.Instance;
            if (cm == null || cm.IsServer)
            {
                return;
            }

            try
            {
                int hours = GetHours(__instance);
                cm.SendToServer(NetPackageManager.GetPackage<NetPackageConsoleCmdServer>().Setup(CommandPrefix + hours), true);
                Log.Out($"[WakaSleepWindowAlign] Sent dedicated sleep request: {hours}h.");
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSleepWindowAlign] Failed to send dedicated sleep request: " + e);
            }
        }

        private static int GetHours(object dialog)
        {
            var field = dialog.GetType().GetField("hours", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                return DawnHelper.GetHoursUntilDawn();
            }
            return Math.Max(1, Math.Min(TFDSleepDialog_Clamp_Transpiler.MaxHours, (int)field.GetValue(dialog)));
        }

        private static Assembly SleepAssembly()
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var name = asm.GetName().Name;
                if (name == "Bedroll Sleeping" || name == "BedrollSleeping")
                {
                    return asm;
                }
            }
            return null;
        }
    }

    [HarmonyPatch(typeof(ConnectionManager), nameof(ConnectionManager.ServerConsoleCommand))]
    public static class ServerConsoleCommand_SleepBridge
    {
        private const string CommandPrefix = "waka_sleep_skip ";

        public static bool Prefix(ClientInfo _cInfo, string _cmd)
        {
            if (string.IsNullOrWhiteSpace(_cmd) || !_cmd.StartsWith(CommandPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                {
                    return false;
                }
                if (!int.TryParse(_cmd.Substring(CommandPrefix.Length).Trim(), out int hours))
                {
                    return false;
                }

                hours = Math.Max(1, Math.Min(TFDSleepDialog_Clamp_Transpiler.MaxHours, hours));
                var world = GameManager.Instance?.World;
                if (world == null)
                {
                    return false;
                }

                ulong target = world.worldTime + (ulong)(hours * 1000);
                world.SetTimeJump(target);
                Log.Out($"[WakaSleepWindowAlign] Dedicated sleep advanced time by {hours}h for {_cInfo?.playerName ?? "unknown"} -> {target}.");
                return false;
            }
            catch (Exception e)
            {
                Log.Warning("[WakaSleepWindowAlign] Dedicated sleep command failed: " + e);
                return false;
            }
        }
    }
}

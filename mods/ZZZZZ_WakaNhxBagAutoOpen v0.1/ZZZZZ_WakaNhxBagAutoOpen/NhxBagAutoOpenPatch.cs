using HarmonyLib;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace WakaNhxBagAutoOpen
{
    // NHX_BackpacksAndBags exposes its open/close path only through
    // internal static methods on NHXBackpacksAndBagsManager. We resolve
    // them once via reflection so we never need a compile-time reference
    // to the foreign assembly (and stay tolerant of NHX being absent).
    internal static class NhxApi
    {
        public static MethodInfo OpenEquippedBag;
        public static MethodInfo ResetLootingState;
        public static MethodInfo IsBagOpenForUi;
        public static MethodInfo TryGetEquippedBag;
        public static bool Available;

        static NhxApi()
        {
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "NHX_BackpacksAndBags");
                if (asm == null)
                {
                    Log.Warning("[WakaNhxBagAutoOpen] NHX_BackpacksAndBags assembly not loaded, patches will be inert.");
                    return;
                }
                var t = asm.GetType("NHX_BackpacksAndBags.NHXBackpacksAndBagsManager");
                if (t == null)
                {
                    Log.Warning("[WakaNhxBagAutoOpen] NHXBackpacksAndBagsManager type not found.");
                    return;
                }
                OpenEquippedBag    = t.GetMethod("OpenEquippedBag",    BindingFlags.NonPublic | BindingFlags.Static);
                ResetLootingState  = t.GetMethod("ResetLootingState",  BindingFlags.NonPublic | BindingFlags.Static);
                IsBagOpenForUi     = t.GetMethod("IsBagOpenForUi",     BindingFlags.NonPublic | BindingFlags.Static);
                TryGetEquippedBag  = t.GetMethod("TryGetEquippedBag",  BindingFlags.NonPublic | BindingFlags.Static);
                Available = OpenEquippedBag != null && ResetLootingState != null;
                if (!Available)
                {
                    Log.Warning("[WakaNhxBagAutoOpen] Required NHX methods not resolved; patches will be inert.");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[WakaNhxBagAutoOpen] NHX API resolve failed: " + e);
            }
        }

        public static bool IsBagOpen(LocalPlayerUI playerUi)
        {
            if (IsBagOpenForUi == null || playerUi == null) return false;
            try { return (bool)IsBagOpenForUi.Invoke(null, new object[] { playerUi }); }
            catch { return false; }
        }

        public static bool HasEquippedBag(EntityPlayerLocal player)
        {
            if (TryGetEquippedBag == null || player == null) return false;
            try
            {
                var args = new object[] { player, null };
                return (bool)TryGetEquippedBag.Invoke(null, args);
            }
            catch { return false; }
        }
    }

    [HarmonyPatch]
    public static class WakaNhxBagOnOpenPatch
    {
        public static bool Prepare() => NhxApi.Available && TargetMethod() != null;

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(XUiC_BackpackWindow), "OnOpen");
        }

        public static void Postfix(XUiC_BackpackWindow __instance)
        {
            try
            {
                var xui      = __instance?.xui;
                var playerUi = xui?.playerUI;
                var player   = playerUi?.entityPlayer as EntityPlayerLocal;
                if (xui == null || playerUi == null || player == null) return;

                // BackpackWindow opens both standalone (Tab) and as a sibling
                // of looting / trader / vending / workstation windows.
                // Auto-bag-open must fire only for the standalone case —
                // otherwise we clobber the foreign loot container or shove
                // the trader chrome out of the way.
                ForeignWindowGuard.LogAllWindowNamesOnce(playerUi);

                if (xui.lootContainer != null) return;
                if (ForeignWindowGuard.IsAnyForeignWindowOpen(playerUi)) return;

                if (NhxApi.IsBagOpen(playerUi)) return;
                if (!NhxApi.HasEquippedBag(player)) return;

                ForeignWindowGuard.LogOpenedOnAutoOpen(playerUi, "BackpackWindow.OnOpen postfix");

                NhxApi.OpenEquippedBag.Invoke(null, new object[] { player });
            }
            catch (Exception e)
            {
                Log.Warning("[WakaNhxBagAutoOpen] OnOpen postfix failed: " + e.Message);
            }
        }
    }

    internal static class ForeignWindowGuard
    {
        // Vanilla window IDs that mean BackpackWindow opened as a sibling
        // (loot / trader / vending / workstation), not as a Tab keystroke.
        // GUIWindowManager.IsWindowOpen(string) returns false for unknown
        // names so over-listing is safe.
        static readonly string[] _foreignIds = new[]
        {
            "looting",
            "trader",
            "vendingMachine",
            "vendingMachineEdit",
            "vehicleStorage",
            "vehicle",
            "drone",
            "workstation_campfire",
            "workstation_forge",
            "workstation_workbench",
            "workstation_chemistrystation",
            "workstation_cementmixer",
            "workstation_helipad",
            "workstation",
        };

        // GUIWindowManager.IsWindowOpen(string) -> bool
        static MethodInfo _isWindowOpen;
        // GUIWindowManager.nameToWindowMap : Dictionary<string,GUIWindow>
        static FieldInfo   _nameToWindowMapField;
        static bool _resolved;
        static bool _allNamesLogged;
        static int  _autoOpenLogCount;

        public static bool IsAnyForeignWindowOpen(LocalPlayerUI playerUi)
        {
            try
            {
                var wm = playerUi?.windowManager;
                if (wm == null) return false;

                EnsureResolved(wm);
                if (_isWindowOpen == null) return false;

                // First pass: hard-coded foreign IDs we know about.
                for (int i = 0; i < _foreignIds.Length; i++)
                {
                    try
                    {
                        if ((bool)_isWindowOpen.Invoke(wm, new object[] { _foreignIds[i] })) return true;
                    }
                    catch { }
                }

                // Second pass: enumerate every registered window name and
                // skip the ones we explicitly know are inert (backpack itself,
                // toolbelt, hud, etc.). This is the safety net that catches
                // workstation / vending IDs we may not have hard-coded.
                if (_nameToWindowMapField != null)
                {
                    var dict = _nameToWindowMapField.GetValue(wm) as IDictionary;
                    if (dict != null)
                    {
                        foreach (var k in dict.Keys)
                        {
                            string name = k as string;
                            if (string.IsNullOrEmpty(name)) continue;
                            if (IsBenignWindow(name)) continue;
                            try
                            {
                                if ((bool)_isWindowOpen.Invoke(wm, new object[] { name })) return true;
                            }
                            catch { }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // Windows that may be open at any time (including during plain Tab)
        // and must not block our auto-bag-open. Anything else that's open
        // means BackpackWindow opened as a sibling of a foreign window.
        static bool IsBenignWindow(string id)
        {
            switch (id)
            {
                case "backpack":
                case "toolbelt":
                case "HUD":
                case "hud":
                case "crafting":
                case "skills":
                case "character":
                case "characterInfoQuests":
                case "characterInfoStats":
                case "windowQuestTracker":
                case "questTracker":
                case "questTrackerOnGameStart":
                case "infoBox":
                case "compass":
                case "minibikeStorage":
                    return true;
            }
            return false;
        }

        static void EnsureResolved(object wm)
        {
            if (_resolved) return;
            try
            {
                var t = wm.GetType();
                _isWindowOpen = t.GetMethod("IsWindowOpen", new[] { typeof(string) });
                _nameToWindowMapField = t.GetField("nameToWindowMap", BindingFlags.NonPublic | BindingFlags.Instance);

                if (_isWindowOpen == null)
                {
                    DumpManagerMembers(t);
                    Log.Warning("[WakaNhxBagAutoOpen] IsWindowOpen(string) not found on " + t.Name + "; foreign-window guard disabled.");
                }
                else
                {
                    Log.Out("[WakaNhxBagAutoOpen][diag] IsWindowOpen(string) resolved on " + t.Name);
                }

                if (_nameToWindowMapField == null)
                {
                    Log.Warning("[WakaNhxBagAutoOpen] nameToWindowMap not found; second-pass guard limited to hard-coded IDs.");
                }
            }
            finally { _resolved = true; }
        }

        // Diagnostic: list every window currently open at the moment
        // auto-bag-open is about to fire. Capped to 5 invocations so we
        // can compare Tab vs B-key opens without spamming the log.
        public static void LogOpenedOnAutoOpen(LocalPlayerUI playerUi, string trigger)
        {
            if (_autoOpenLogCount >= 5) return;
            _autoOpenLogCount++;
            try
            {
                var wm = playerUi?.windowManager;
                if (wm == null) return;
                EnsureResolved(wm);
                if (_nameToWindowMapField == null || _isWindowOpen == null) return;
                var dict = _nameToWindowMapField.GetValue(wm) as IDictionary;
                if (dict == null) return;
                var openNames = new System.Collections.Generic.List<string>();
                foreach (var k in dict.Keys)
                {
                    string name = k as string;
                    if (string.IsNullOrEmpty(name)) continue;
                    try
                    {
                        if ((bool)_isWindowOpen.Invoke(wm, new object[] { name }))
                            openNames.Add(name);
                    }
                    catch { }
                }
                openNames.Sort(StringComparer.OrdinalIgnoreCase);
                Log.Out("[WakaNhxBagAutoOpen][diag] auto-open candidate #" + _autoOpenLogCount + " (" + trigger + "). currently open=[" + string.Join(",", openNames.ToArray()) + "]");
            }
            catch (Exception e) { Log.Warning("[WakaNhxBagAutoOpen][diag] LogOpenedOnAutoOpen failed: " + e.Message); }
        }

        // Diagnostic: list every registered window name once. Helps confirm
        // which IDs to add to _foreignIds / IsBenignWindow if a future
        // workstation/vending variant slips through.
        public static void LogAllWindowNamesOnce(LocalPlayerUI playerUi)
        {
            if (_allNamesLogged) return;
            try
            {
                var wm = playerUi?.windowManager;
                if (wm == null) return;
                EnsureResolved(wm);
                if (_nameToWindowMapField == null) return;
                var dict = _nameToWindowMapField.GetValue(wm) as IDictionary;
                if (dict == null) return;
                var keys = new System.Collections.Generic.List<string>();
                foreach (var k in dict.Keys) keys.Add(k?.ToString() ?? "(null)");
                keys.Sort(StringComparer.OrdinalIgnoreCase);
                Log.Out("[WakaNhxBagAutoOpen][diag] all registered window names: [" + string.Join(",", keys.ToArray()) + "]");
            }
            catch (Exception e) { Log.Warning("[WakaNhxBagAutoOpen][diag] LogAllWindowNamesOnce failed: " + e.Message); }
            finally { _allNamesLogged = true; }
        }

        static void DumpManagerMembers(Type t)
        {
            try
            {
                Log.Out("[WakaNhxBagAutoOpen][diag-mem] === XUiWindowManager type: " + t.FullName + " ===");
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    Log.Out("[WakaNhxBagAutoOpen][diag-mem] field " + f.FieldType.Name + " " + f.Name);
                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    Log.Out("[WakaNhxBagAutoOpen][diag-mem] prop  " + p.PropertyType.Name + " " + p.Name);
                foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var n = m.Name;
                    if (n.IndexOf("Open", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Close", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("IsWindow", StringComparison.OrdinalIgnoreCase) >= 0
                        || n.IndexOf("Active", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        Log.Out("[WakaNhxBagAutoOpen][diag-mem] meth  " + m.ReturnType.Name + " " + n + "(" + string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name).ToArray()) + ")");
                    }
                }
            }
            catch (Exception e) { Log.Warning("[WakaNhxBagAutoOpen][diag-mem] dump failed: " + e.Message); }
        }

        static string GetId(object window)
        {
            var t = window.GetType();
            var prop = t.GetProperty("ID") ?? t.GetProperty("Id") ?? t.GetProperty("id");
            if (prop != null) return prop.GetValue(window) as string;
            var fld = t.GetField("ID") ?? t.GetField("Id") ?? t.GetField("id");
            if (fld != null) return fld.GetValue(window) as string;
            return null;
        }
    }

    [HarmonyPatch]
    public static class WakaNhxBagOnClosePatch
    {
        public static bool Prepare() => NhxApi.Available && TargetMethod() != null;

        public static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(XUiC_BackpackWindow), "OnClose");
        }

        // Prefix runs before the backpack window finishes closing so the bag
        // close path can piggy-back on the same Tab keystroke and the close
        // animations stay synced.
        public static void Prefix(XUiC_BackpackWindow __instance)
        {
            try
            {
                var playerUi = __instance?.xui?.playerUI;
                if (playerUi == null) return;
                if (!NhxApi.IsBagOpen(playerUi)) return;

                NhxApi.ResetLootingState.Invoke(null, new object[] { playerUi, "WakaNhxBagAutoOpen: backpack window closed" });
            }
            catch (Exception e)
            {
                Log.Warning("[WakaNhxBagAutoOpen] OnClose prefix failed: " + e.Message);
            }
        }
    }
}

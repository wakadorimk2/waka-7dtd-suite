using System;

namespace WakaSoloServerPause
{
    public static class ClientEscMenuPauseBridge
    {
        const string InGameMenuWindow = "ingameMenu";

        static bool hasKnownState;
        static bool lastInGameMenuOpen;
        static bool localPauseActive;
        static int localPausedEntityId = -1;
        static ulong localFrozenWorldTime;

        public static bool IsLocalPauseActive => localPauseActive;
        public static ulong LocalFrozenWorldTime => localFrozenWorldTime;

        public static bool IsLocalPausedPlayer(EntityAlive entity)
        {
            return localPauseActive &&
                   entity != null &&
                   localPausedEntityId != -1 &&
                   entity.entityId == localPausedEntityId;
        }

        public static void CheckInGameMenuState(GUIWindowManager manager, string source)
        {
            try
            {
                ConnectionManager connectionManager = SingletonMonoBehaviour<ConnectionManager>.Instance;
                if (connectionManager == null || manager == null)
                {
                    return;
                }

                bool isOpen = manager.IsWindowOpen(InGameMenuWindow);
                if (hasKnownState && isOpen == lastInGameMenuOpen)
                {
                    return;
                }

                if (!hasKnownState && !isOpen)
                {
                    hasKnownState = true;
                    lastInGameMenuOpen = false;
                    return;
                }

                hasKnownState = true;
                lastInGameMenuOpen = isOpen;
                if (connectionManager.IsServer)
                {
                    SetPauseDirectly(isOpen, source);
                }
                else
                {
                    SetLocalPauseState(isOpen, source);
                    SendPauseRequest(isOpen, source);
                }
            }
            catch (Exception e)
            {
                Log.Warning($"[WakaSoloServerPause] Esc menu state check failed ({source}): {e}");
            }
        }

        static void SetPauseDirectly(bool desiredActive, string source)
        {
            string response = PauseManager.SetFromCommand(
                null,
                desiredActive,
                desiredActive ? "esc menu open local server" : "esc menu close local server");

            Log.Out($"[WakaSoloServerPause] Applied local {response} after ingameMenu {(desiredActive ? "open" : "close")} ({source}).");
        }

        static void SetLocalPauseState(bool desiredActive, string source)
        {
            if (desiredActive)
            {
                EntityPlayerLocal player = GameManager.Instance?.World?.GetPrimaryPlayer();
                localPauseActive = true;
                localPausedEntityId = player?.entityId ?? -1;
                localFrozenWorldTime = GameManager.Instance?.World?.worldTime ?? 0UL;
            }
            else
            {
                localPauseActive = false;
                localPausedEntityId = -1;
                localFrozenWorldTime = 0UL;
            }

            Log.Out($"[WakaSoloServerPause] Local time {(desiredActive ? "freeze" : "resume")} after ingameMenu {(desiredActive ? "open" : "close")} ({source}); local entity={localPausedEntityId}, local worldTime={localFrozenWorldTime}.");
        }

        static void SendPauseRequest(bool desiredActive, string source)
        {
            World world = GameManager.Instance?.World;
            EntityPlayerLocal player = world?.GetPrimaryPlayer();
            if (player == null)
            {
                return;
            }

            string command = desiredActive ? "/waka_pause_on" : "/waka_pause_off";
            SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(
                NetPackageManager.GetPackage<NetPackageChat>().Setup(
                    EChatType.Global,
                    player.entityId,
                    command,
                    null,
                    EMessageSender.SenderIdAsPlayer,
                    GeneratedTextManager.BbCodeSupportMode.Supported));

            Log.Out($"[WakaSoloServerPause] Sent {command} after ingameMenu {(desiredActive ? "open" : "close")} ({source}).");
        }
    }
}

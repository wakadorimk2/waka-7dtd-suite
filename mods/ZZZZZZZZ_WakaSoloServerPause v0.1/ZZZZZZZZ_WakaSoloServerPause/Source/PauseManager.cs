using System;
using System.Collections.Generic;

namespace WakaSoloServerPause
{
    public static class PauseManager
    {
        const string Prefix = "[WakaSoloServerPause]";

        static readonly Dictionary<int, PlayerPauseState> states = new Dictionary<int, PlayerPauseState>();
        static bool active;
        static int activeEntityId = -1;
        static bool savedDebugStopEnemiesMoving;
        static bool hasSavedDebugStopEnemiesMoving;
        static float nextPollTime;
        static ulong frozenWorldTime;

        public static bool IsActive => active;
        public static ulong FrozenWorldTime => frozenWorldTime;

        public static bool IsEntityPaused(EntityAlive entity)
        {
            return active && entity != null && entity.entityId == activeEntityId;
        }

        public static bool ShouldPauseBuffTick(EntityAlive entity)
        {
            return active && entity != null;
        }

        public static bool ShouldPauseEntitySystem(EntityAlive entity)
        {
            return active && entity != null;
        }

        public static string ToggleFromCommand(ClientInfo sender, string source)
        {
            try
            {
                if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                {
                    return "Waka pause can only be toggled on the server.";
                }

                int connected = CountConnectedPlayers();
                if (active)
                {
                    Deactivate("manual toggle", true);
                    return "AFK safety mode OFF.";
                }

                if (connected != 1)
                {
                    return $"AFK safety mode can only start with exactly one connected player. Current players: {connected}.";
                }

                EntityPlayer player = ResolveTargetPlayer(sender);
                if (player == null)
                {
                    return "AFK safety mode could not find the connected player entity yet.";
                }

                Activate(player, source);
                return "AFK safety mode ON. Incoming damage and survival stat ticking are paused.";
            }
            catch (Exception e)
            {
                Log.Warning($"{Prefix} Toggle failed: {e}");
                return "AFK safety mode failed. Check the server log for WakaSoloServerPause.";
            }
        }

        public static string SetFromCommand(ClientInfo sender, bool desiredActive, string source)
        {
            try
            {
                if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
                {
                    return "Waka pause can only be changed on the server.";
                }

                if (desiredActive)
                {
                    if (active)
                    {
                        Log.Out($"{Prefix} Already enabled by {source}; no change.");
                        return "AFK safety mode is already ON.";
                    }

                    int connected = CountConnectedPlayers();
                    if (connected != 1)
                    {
                        Log.Out($"{Prefix} Enable by {source} rejected; connected players: {connected}.");
                        return $"AFK safety mode can only start with exactly one connected player. Current players: {connected}.";
                    }

                    EntityPlayer player = ResolveTargetPlayer(sender);
                    if (player == null)
                    {
                        Log.Out($"{Prefix} Enable by {source} failed; player entity unavailable.");
                        return "AFK safety mode could not find the connected player entity yet.";
                    }

                    Activate(player, source);
                    return "AFK safety mode ON. Incoming damage and survival stat ticking are paused.";
                }

                if (!active)
                {
                    Log.Out($"{Prefix} Already disabled by {source}; no change.");
                    return "AFK safety mode is already OFF.";
                }

                Deactivate(source, true);
                return $"AFK safety mode OFF ({source}).";
            }
            catch (Exception e)
            {
                Log.Warning($"{Prefix} Set failed ({source}, desiredActive={desiredActive}): {e}");
                return "AFK safety mode failed. Check the server log for WakaSoloServerPause.";
            }
        }

        public static void OnGameUpdate(ref ModEvents.SGameUpdateData data)
        {
            try
            {
                if (!active)
                {
                    return;
                }

                if (UnityEngine.Time.realtimeSinceStartup >= nextPollTime)
                {
                    nextPollTime = UnityEngine.Time.realtimeSinceStartup + 1f;
                    int connected = CountConnectedPlayers();
                    if (connected != 1)
                    {
                        Deactivate($"connected players changed to {connected}", true);
                        return;
                    }
                }

                EntityPlayer player = GetActivePlayer();
                if (player == null || player.IsDead())
                {
                    Deactivate("player entity unavailable", true);
                    return;
                }

                ApplySafety(player);
            }
            catch (Exception e)
            {
                Log.Warning($"{Prefix} OnGameUpdate failed: {e}");
            }
        }

        public static void OnGameShutdown(ref ModEvents.SGameShutdownData data)
        {
            if (active)
            {
                Deactivate("game shutdown", false);
            }
        }

        public static void SendPrivateMessage(ClientInfo client, string message)
        {
            if (client == null)
            {
                Log.Out($"{Prefix} {message}");
                return;
            }

            client.SendPackage(NetPackageManager.GetPackage<NetPackageSimpleChat>().Setup(message));
        }

        static void Activate(EntityPlayer player, string source)
        {
            active = true;
            activeEntityId = player.entityId;
            frozenWorldTime = GameManager.Instance?.World?.worldTime ?? 0UL;
            nextPollTime = 0f;

            states[player.entityId] = PlayerPauseState.Capture(player);
            if (!hasSavedDebugStopEnemiesMoving)
            {
                savedDebugStopEnemiesMoving = GamePrefs.GetBool(EnumGamePrefs.DebugStopEnemiesMoving);
                hasSavedDebugStopEnemiesMoving = true;
            }

            GamePrefs.Set(EnumGamePrefs.DebugStopEnemiesMoving, true);
            ApplySafety(player);
            Log.Out($"{Prefix} Enabled by {source} for entity {player.entityId} ({player.EntityName}); frozen worldTime={frozenWorldTime}.");
        }

        static void Deactivate(string reason, bool notify)
        {
            int entityId = activeEntityId;
            EntityPlayer player = GetActivePlayer();

            if (player != null && states.TryGetValue(entityId, out PlayerPauseState state))
            {
                state.Restore(player);
            }

            if (hasSavedDebugStopEnemiesMoving)
            {
                GamePrefs.Set(EnumGamePrefs.DebugStopEnemiesMoving, savedDebugStopEnemiesMoving);
            }

            active = false;
            activeEntityId = -1;
            frozenWorldTime = 0UL;
            states.Remove(entityId);
            hasSavedDebugStopEnemiesMoving = false;

            Log.Out($"{Prefix} Disabled: {reason}.");
            if (notify)
            {
                ClientInfo client = SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.ForEntityId(entityId);
                SendPrivateMessage(client, $"AFK safety mode OFF ({reason}).");
            }
        }

        static void ApplySafety(EntityPlayer player)
        {
            player.SetIgnoredByAI(true);
            if (player.Stats != null)
            {
                player.Stats.Health.RegenerationAmount = 0f;
                player.Stats.Stamina.RegenerationAmount = 0f;
                if (player.Stats.Water != null) player.Stats.Water.RegenerationAmount = 0f;
                if (player.Stats.Food != null) player.Stats.Food.RegenerationAmount = 0f;
            }
            GamePrefs.Set(EnumGamePrefs.DebugStopEnemiesMoving, true);
        }

        static EntityPlayer ResolveTargetPlayer(ClientInfo sender)
        {
            World world = GameManager.Instance?.World;
            if (world == null)
            {
                return null;
            }

            if (sender != null && sender.entityId != -1)
            {
                return world.GetEntity(sender.entityId) as EntityPlayer;
            }

            foreach (ClientInfo client in SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.List)
            {
                if (IsConnectedPlayer(client))
                {
                    return world.GetEntity(client.entityId) as EntityPlayer;
                }
            }

            return world.GetPrimaryPlayer();
        }

        static EntityPlayer GetActivePlayer()
        {
            World world = GameManager.Instance?.World;
            return world?.GetEntity(activeEntityId) as EntityPlayer;
        }

        static int CountConnectedPlayers()
        {
            int count = 0;
            foreach (ClientInfo client in SingletonMonoBehaviour<ConnectionManager>.Instance.Clients.List)
            {
                if (IsConnectedPlayer(client))
                {
                    count++;
                }
            }

            if (count == 0 && GameManager.Instance?.World?.GetPrimaryPlayer() != null)
            {
                return 1;
            }

            return count;
        }

        static bool IsConnectedPlayer(ClientInfo client)
        {
            return client != null &&
                   client.loginDone &&
                   client.bAttachedToEntity &&
                   client.entityId != -1 &&
                   !client.disconnecting;
        }

        struct PlayerPauseState
        {
            public bool IgnoredByAI;

            public static PlayerPauseState Capture(EntityPlayer player)
            {
                return new PlayerPauseState
                {
                    IgnoredByAI = player.IsIgnoredByAI()
                };
            }

            public void Restore(EntityPlayer player)
            {
                player.SetIgnoredByAI(IgnoredByAI);
            }
        }
    }
}

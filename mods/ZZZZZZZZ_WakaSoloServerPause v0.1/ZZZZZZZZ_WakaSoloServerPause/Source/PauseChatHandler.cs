using System;

namespace WakaSoloServerPause
{
    public static class PauseChatHandler
    {
        public static ModEvents.EModEventResult OnChatMessage(ref ModEvents.SChatMessageData data)
        {
            string msg = data.Message == null ? string.Empty : data.Message.Trim();
            if (msg.Equals("/pause", StringComparison.OrdinalIgnoreCase) ||
                msg.Equals("/waka_pause", StringComparison.OrdinalIgnoreCase))
            {
                string response = PauseManager.ToggleFromCommand(data.ClientInfo, "chat");
                PauseManager.SendPrivateMessage(data.ClientInfo, response);
                return ModEvents.EModEventResult.StopHandlersAndVanilla;
            }

            if (msg.Equals("/waka_pause_on", StringComparison.OrdinalIgnoreCase))
            {
                string response = PauseManager.SetFromCommand(data.ClientInfo, true, "esc menu open");
                PauseManager.SendPrivateMessage(data.ClientInfo, response);
                return ModEvents.EModEventResult.StopHandlersAndVanilla;
            }

            if (msg.Equals("/waka_pause_off", StringComparison.OrdinalIgnoreCase))
            {
                string response = PauseManager.SetFromCommand(data.ClientInfo, false, "esc menu close");
                PauseManager.SendPrivateMessage(data.ClientInfo, response);
                return ModEvents.EModEventResult.StopHandlersAndVanilla;
            }

            return ModEvents.EModEventResult.Continue;
        }
    }
}

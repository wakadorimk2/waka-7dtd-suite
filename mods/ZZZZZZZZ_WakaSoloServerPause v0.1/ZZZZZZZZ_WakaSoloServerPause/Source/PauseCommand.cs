using System.Collections.Generic;
using UnityEngine.Scripting;

namespace WakaSoloServerPause
{
    [Preserve]
    public class ConsoleCmdWakaSoloPause : ConsoleCmdAbstract
    {
        public override int DefaultPermissionLevel => 1000;

        public override string[] getCommands()
        {
            return new[] { "pause", "waka_pause" };
        }

        public override string getDescription()
        {
            return "Toggle Waka solo-server AFK safety mode.";
        }

        public override string getHelp()
        {
            return "Usage: pause | waka_pause\nToggles AFK safety mode while exactly one player is connected.";
        }

        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            ClientInfo sender = _senderInfo.RemoteClientInfo;
            string message = PauseManager.ToggleFromCommand(sender, "console");
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output(message);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using TankLib.STU.Types;

namespace DataTool.DataModels.Chat {
    public class ChatSettings {
        public IEnumerable<ChatChannel> Channels { get; set; }
        public IEnumerable<ChatCommand> Commands { get; set; }

        public ChatSettings(STUGenericSettings_Chat chatSettings) {
            Channels = chatSettings.m_chatChannels.Select(x => new ChatChannel(x));
            Commands = chatSettings.m_chatCommands.Select(x => new ChatCommand(x));
        }
    }
}

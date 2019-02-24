using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using DataTool.DataModels.Chat;
using TankLib.STU.Types;

namespace DataTool.DataModels.Chat {
    [DataContract]
    public class ChatSettings {
        [DataMember]
        public IEnumerable<ChatChannel> Channels;
        
        [DataMember]
        public IEnumerable<ChatCommand> Commands;
        
        public ChatSettings(STUGenericSettings_Chat chatSettings) {
            Channels = chatSettings.m_chatChannels.Select(x => new ChatChannel(x));
            Commands = chatSettings.m_chatCommands.Select(x => new ChatCommand(x));
        }
    }
}
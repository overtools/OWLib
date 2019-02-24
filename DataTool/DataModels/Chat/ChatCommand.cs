using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Chat {
    [DataContract]
    public class ChatCommand {
        [DataMember]
        public string Name;

        [DataMember]
        public string Description;

        [DataMember]
        public STUChatCommandType Type;

        [DataMember]
        public IEnumerable<string> Aliases;

        public ChatCommand(STUChatCommand command) {
            Name = GetString(command.m_4CED72F5);
            Description = GetString(command.m_commandDescription);
            Type = command.m_chatCommandType;
            Aliases = command.m_chatCommandAliases.Select(x => GetString(x));
        }
    }
}
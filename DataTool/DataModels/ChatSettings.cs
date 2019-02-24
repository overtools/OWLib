using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using TankLib;
using TankLib.Math;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels {
    [DataContract]
    public class ChatSettings {
        [DataMember]
        public IEnumerable<Channel> Channels;
        
        [DataMember]
        public IEnumerable<Command> Commands;
        
        public ChatSettings(STUGenericSettings_Chat chatSettings) {
            Channels = chatSettings.m_chatChannels.Select(x => new Channel(x));
            Commands = chatSettings.m_chatCommands.Select(x => new Command(x));
        }
    }
    
    [DataContract]
    public class Command {
        [DataMember]
        public string Name;

        [DataMember]
        public string Description;

        [DataMember]
        public STUChatCommandType Type;

        [DataMember]
        public IEnumerable<string> Aliases;

        public Command(STUChatCommand command) {
            Name = GetString(command.m_4CED72F5);
            Description = GetString(command.m_commandDescription);
            Type = command.m_chatCommandType;
            Aliases = command.m_chatCommandAliases.Select(x => GetString(x));
        }
    }

    [DataContract]
    public class Channel {
        [DataMember]
        public string Name;

        [DataMember]
        public teColorRGB Color;

        [DataMember]
        public Enum_5D8C1DCC Type;

        public Channel(STUChatChannelDefinition channel) {
            Name = GetString(channel.m_chatChannelName);
            Color = channel.m_chatChannelColor;
            Type = channel.m_chatChannelType;
        }
    }
}
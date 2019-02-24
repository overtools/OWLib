using System.Runtime.Serialization;
using TankLib.Math;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Chat {
    [DataContract]
    public class ChatChannel {
        [DataMember]
        public string Name;

        [DataMember]
        public teColorRGB Color;

        [DataMember]
        public Enum_5D8C1DCC Type;

        public ChatChannel(STUChatChannelDefinition channel) {
            Name = GetString(channel.m_chatChannelName);
            Color = channel.m_chatChannelColor;
            Type = channel.m_chatChannelType;
        }
    }
}
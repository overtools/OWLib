using TankLib.Math;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Chat;

public class ChatChannel {
    public string Name { get; set; }
    public teColorRGB Color { get; set; }
    public STUChatChannelType Type { get; set; }

    public ChatChannel(STUChatChannelDefinition channel) {
        Name = GetString(channel.m_chatChannelName);
        Color = channel.m_chatChannelColor;
        Type = channel.m_chatChannelType;
    }
}
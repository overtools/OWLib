using System.Collections.Generic;
using System.Linq;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;

namespace DataTool.DataModels.Chat;

public class ChatCommand {
    public string Name { get; set; }
    public string Description { get; set; }
    public STUChatCommandType Type { get; set; }
    public IEnumerable<string> Aliases { get; set; }

    public ChatCommand(STUChatCommand command) {
        Name = GetString(command.m_4CED72F5);
        Description = GetString(command.m_commandDescription);
        Type = command.m_chatCommandType;
        Aliases = command.m_chatCommandAliases.Select(x => GetString(x));
    }
}
using System.Collections.Generic;
using DataTool.DataModels.Chat;
using DataTool.Flag;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-chat-settings", Description = "List chat settings", CustomFlags = typeof(ListFlags))]
    public class ListChatSettings : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var chatData = GetChatData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(chatData, flags);
                    return;
                }

            foreach (var chatGroup in chatData) {
               Log("Channels:");
               foreach (var channel in chatGroup.Channels) {
                   Log($"\t{channel.Name} ({channel.Type})");
               }
               
               Log("Commands:");
               foreach (var command in chatGroup.Commands) {
                   Log($"\t{command.Name} ({command.Type})");
                   Log($"\t\t{command.Description}");
                   Log($"\t\t{string.Join(", ", command.Aliases)}");
               }
            }
        }

        private static List<ChatSettings> GetChatData() {
            var chatSettings = new List<ChatSettings>();
            
            foreach (ulong key in TrackedFiles[0x54]) {
                STUGenericSettings_Chat chatGroup = GetInstance<STUGenericSettings_Chat>(key);
                if (chatGroup == null) continue;

                var chat = new ChatSettings(chatGroup);
                chatSettings.Add(chat);
            }

            return chatSettings;
        }
    }
}
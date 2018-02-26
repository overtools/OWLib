using System;
using System.Linq;
using DataTool.Flag;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List {
    [Tool("list-chat-container", Description = "List Chat Stuff", TrackTypes = new ushort[] {0x54}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListChatStuff : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (ulong key in TrackedFiles[0x54]) {
                var data = GetInstance<STUChatContainer>(key);
                if (data == null) continue;

                if (data.ChannelDefinitions != null) {
                    Log("Channels:");
                    foreach (var channel in data.ChannelDefinitions) {
                        Log($"\t Name: {GetString(channel.Name)}");
                        Log($"\t Type: {channel.Type}");
                        Log($"\tColor: {channel.Color.Hex()}");
                        Log("");
                    }
                    Log("");
                }

                if (data.ChatCommands != null) {
                    Log("Chat Commands:");
                    foreach (var chat in data.ChatCommands) {
                        Log($"\tCommand: {GetString(chat.Name)}");
                        Log($"\tSubline: {GetString(chat.Subline)}");
                        Log($"\t   Type: {chat.Type}");
                        

                        var triggers = chat.Triggers.Select(c => GetString(c));
                        Log($"\tTrigger: [ {string.Join(", ", triggers)} ]");
                        Log("");
                    }
                }
            }
        }
    }
}
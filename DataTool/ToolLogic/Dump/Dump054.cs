using System;
using System.Diagnostics;
using DataTool.Flag;
using DataTool.Helper;
using STULib.Types.ZeroFiveFour;
using STULib.Types;
using System.Collections.Generic;
using System.Linq;
using STULib.Types.Chat;
using STULib.Types.Lootboxes;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-054", Description = "Dumps all the strings", IsSensitive = true, TrackTypes = new ushort[] { 0x54 }, CustomFlags = typeof(DumpFlags))]
    public class Dump054 : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            foreach (var key in TrackedFiles[0x54]) {
                var indent = new IndentHelper();
                var thing = GetInstance<STU_866672AD>(key);

                switch (thing) {
                    case STU_4BD859E5 c:
                        //Debugger.Break();
                        break;
                    case STU_6C2411B9 c:
                        //Debugger.Break();
                        break;
                    case STU_B7148D95 c:
                        //Debugger.Break();
                        break;
                    case STU_7725B6D6 c:
                        //Debugger.Break();
                        break;
                    case STUChatContainer c:
                        Log($"STUChatContainer");
                        Log($"{indent+1}Message Groups:");
                        foreach (var messageGroup in c.ChannelDefinitions) {
                            string name = GetString(messageGroup.Name);
                            Log($"{indent+2}{name} ({messageGroup.Type}) - {messageGroup.Color.Hex()}");
                        }
                        Log($"\n {indent+1}Chat Commands:");
                        foreach (var chatCommand in c.ChatCommands) {
                            string name = GetString(chatCommand.Name);
                            string desc = GetString(chatCommand.Subline);
                            List<string> triggers = chatCommand.Triggers.Select(trigger => GetString(trigger)).ToList();
                            
                            Log($"{indent+2}{name}:");
                            Log($"{indent+3}Desc: {desc}");
                            Log($"{indent+3}Type: {chatCommand.Type}");
                            Log($"{indent+3}Triggers: {string.Join(", ", triggers)}");
                        }
                        Log();
                        break;
                    case STU_5CE04BB1 c:
                        //Debugger.Break();
                        break;
                    case STULootboxDefinitionContainer c:
                        Log("STULootboxDefinitionContainer");
                        foreach (var lootbox in c.Events) {
                            Log($"{indent+1}{lootbox.Event} Lootboxes:");
                            foreach (var rarity in lootbox.RarityCosts) {
                                Log($"{indent+2}{rarity.Rarity} - {rarity.ItemCost} | {rarity.DupeValue}");
                            }
                        }
                        break;
                    default:
                        //Debugger.Break();
                        break;
                }

            }
        }

    }
}
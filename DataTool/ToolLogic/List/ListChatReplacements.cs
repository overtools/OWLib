/*using System;
using System.Collections.Generic;
using DataTool.Flag;
using DataTool.Helper;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;


namespace DataTool.ToolLogic.List {
    [Tool("list-chat-replacements", Description = "List chat replacements", TrackTypes = new ushort[] {0x54, 0x7F}, CustomFlags = typeof(ListFlags))]
    public class ListChatReplacements : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            var iL = new IndentHelper();
            var keyCache = new List<ulong>();

            Log("Chat Replacement Strings");
            foreach (var key in TrackedFiles[0x54]) {
                var data = GetInstance<STUChatReplacementContainer>(key);
                if (data == null) continue;

                Log($"{iL+1}Replacements:");
                var i = 0;
                foreach (var replacement in data.Replacements) {
                    
                    Log($"{iL+2}[{i}] Replacement:");
                    i++;

                    var triggers = replacement.Triggers != null ? GetInstance<STUChatReplacementStrings>(replacement.Triggers) : null;
                    if (triggers != null) {
                        keyCache.Add(replacement.Triggers);

                        Log($"{iL+3}Triggers:");
                        foreach (var trigger in triggers.ToReplace.GetStrings()) {
                            Log($"{iL+4}{trigger}");
                        }
                    }

                    if (replacement.GlobalReplacementStrings != null) {
                        Log($"{iL+3}Global Replacements:");
                        foreach (var guid in replacement.GlobalReplacementStrings) {
                            var s = GetString(guid);
                            if (s != null) Log($"{iL+4}{s}");
                        }
                    }

                    if (replacement.Heroes != null) {
                        Log($"{iL+3}Heroes:");
                        foreach (var guid in replacement.Heroes) {
                            var hero = GetInstance<STUHero>(guid);
                            var heroName = GetString(hero?.Name);
                            if (heroName != null) Log($"{iL + 4}{heroName}");
                        }
                    }

                    if (replacement.HeroChatReplacementStrings != null) {
                        Log($"{iL + 3}Hero Replacements:");
                        foreach (var guid in replacement.HeroChatReplacementStrings) {
                            var s = GetString(guid);
                            if (s != null) Log($"{iL + 4}{s}");
                        }
                    }
                }
            }

            foreach (var key in TrackedFiles[0x7F]) {
                if (keyCache.Contains(key)) continue;

                var data = GetInstance<STUChatReplacementStrings>(key);
                if (data != null) {
                    var strings = data.ToReplace.GetStrings();
                    Log($"\n{iL+1}Profanity Filter:");
                    foreach (var word in strings) {
                        Log($"{iL+2}{word}");
                    }
                }
            }
        }
    }
}*/
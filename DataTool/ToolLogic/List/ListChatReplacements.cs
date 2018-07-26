using System;
using System.Collections.Generic;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.Logger;
using static DataTool.Helper.STUHelper;


namespace DataTool.ToolLogic.List {
    [Tool("list-chat-replacements", Description = "List chat replacements", TrackTypes = new ushort[] {0x54, 0x7F}, CustomFlags = typeof(ListFlags))]
    public class ListChatReplacements : JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public class Replacement {
            public List<string> Triggers;
            public List<string> GlobalReplacements;
            public List<string> Heroes;
            public List<string> HeroReplacements;

            public Replacement(List<string> triggers, List<string> globalReplacements, List<string> heroes, List<string> heroReplacements) {
                Triggers = triggers;
                GlobalReplacements = globalReplacements;
                Heroes = heroes;
                HeroReplacements = heroReplacements;
            }
        }

        public class ChatReplacements {
            public List<Replacement> Replacements = new List<Replacement>();
            public List<string> ProfanityFilter = new List<string>();
        }

        public void Parse(ICLIFlags toolFlags) {
            var iL = new IndentHelper();
            var keyCache = new List<ulong>();

            var replacements = GetChatReplacements();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    ParseJSON(replacements, flags);
                    return;
                }

            Log($"{iL+1}Replacements:");
            var i = 0;
            foreach (var replacement in replacements.Replacements) {
                Log($"{iL+2}[{i}] Replacement:");
                i++;

                if (replacement.Triggers != null) {
                    Log($"{iL+3}Triggers:");
                    foreach (var str in replacement.Triggers) {
                        Log($"{iL+4}{str}");
                    }
                }

                if (replacement.GlobalReplacements != null) {
                    Log($"{iL+3}Global Replacements:");
                    foreach (var str in replacement.GlobalReplacements) {
                        Log($"{iL+4}{str}");
                    }
                }
                
                if (replacement.Heroes != null) {
                    Log($"{iL+3}Heroes:");
                    foreach (var str in replacement.Heroes) {
                        Log($"{iL + 4}{str}");
                    }
                }
                
                if (replacement.HeroReplacements != null) {
                    Log($"{iL + 3}Hero Replacements:");
                    foreach (var str in replacement.HeroReplacements) {
                        Log($"{iL + 4}{str}");
                    }
                }
            }

            Log($"\n{iL+1}Profanity Filter:");
            foreach (var str in replacements.ProfanityFilter) {
                Log($"{iL+2}{str}");
            }
        }

        public ChatReplacements GetChatReplacements() {
            var keyCache = new List<ulong>();
            var @return = new ChatReplacements();

            foreach (var key in TrackedFiles[0x54]) {
                var data = GetInstance<STUChatReplacementContainer>(key);
                if (data == null) continue;
                
                foreach (var item in data.Replacements) {
                    List<string> heroes = item.Heroes?.Select(h => GetInstance<STUHero>(h)).Where(h => h != null).Select(h => GetString(h?.Name)).ToList();
                    List<string> heroReplacements = item.HeroChatReplacementStrings?.Select(s => GetString(s)).ToList();
                    List<string> globalReplacements = item.GlobalReplacementStrings?.Select(s => GetString(s)).ToList();

                    var triggers = new List<string>();
                    var replStrings = item.Triggers != null ? GetInstance<STUChatReplacementStrings>(item.Triggers) : null;
                    if (replStrings != null) {
                        // Add triggers key to a cache so the profanity code below doesn't readd these
                        keyCache.Add(item.Triggers);
                        triggers = replStrings.ToReplace.GetStrings().ToList();
                    }
                    
                    @return.Replacements.Add(new Replacement(triggers, globalReplacements, heroes, heroReplacements));
                }
            }

            foreach (var key in TrackedFiles[0x7F]) {
                // Chat replacement triggers in the code above are included in this list, we don't want to log them here
                if (keyCache.Contains(key)) continue;

                var data = GetInstance<STUChatReplacementStrings>(key);
                if (data != null) {
                    @return.ProfanityFilter = data.ToReplace.GetStrings().ToList();
                }
            }

            return @return;
        }
    }
}
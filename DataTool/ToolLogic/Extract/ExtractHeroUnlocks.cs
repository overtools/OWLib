using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic.Unlock;
using DataTool.WPF;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Extract {
    // syntax:
    // *|skin=Classic
    // *|skin=Classic
    // *|skin=*,!Classic
    // *
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*"
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*" Roadhog
    // "Soldier: 76|skin=Classic,Daredevil: 76|emote=Pushups|victory pose=*" Roadhog|emote=Dance  // assume nothing else for roadhog, be specific
    // "Soldier: 76|skin=!Classic"  // invalid, maybe just do nothing
    // "Soldier: 76|skin=!Classic,*"  // valid
    // "Soldier: 76|skin=(rarity=rare,event=halloween),!Dance"
    // "{hero name}|{type}=({tag name}={tag}),{item name}"
    // Roadhog
    
    // future possibilities. could use the in-game filter system to try and give us better results
    // hero|type=hello
    // hero|skin=classic
    // hero|skin=*,(rarity=common)
    // "hero|skin=(category=overwatch)"
    // "hero|skin=(category=achievements)"
    // "hero|skin=(category=summer games)"
    // "hero|skin=(category=halloween terror)"
    // "hero|skin=(category=winter wonderland)"
    // "hero|skin=(category=lunar new year)"
    // "hero|skin=(category=archives)"
    // "hero|skin=(category=anniversary)"
    // "hero|skin=(category=competitive)"
    // "hero|skin=(category=overwatch league)"
    // "hero|skin=(category=special)"
    // "hero|skin=(category=legacy)"
    
    [DebuggerDisplay("CosmeticType: {" + nameof(Name) + "}")]
    public class CosmeticType : QueryType {
        public CosmeticType(string name) {
            Name = name;
            Tags = new List<QueryTag> {
                new QueryTag("rarity", new List<string>{"common", "rare", "epic", "legendary"}),
                new QueryTag("event", new List<string>{"base", "summergames", "halloween", "winter", "lunarnewyear", "archives", "anniversary"}),
                new QueryTag("leagueTeam", new List<string>(), "none"),
                new QueryTag("special", new List<string> {"sg2018"})
            };
        }
    }
    
    [Tool("extract-unlocks", Name="Hero Cosmetics", Description = "Extract hero cosmetics", CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : QueryParser, IAwareTool, IQueryParser {
        protected virtual string RootDir => "Heroes";
        protected virtual bool NPCs => false;
        public List<QueryType> QueryTypes => new List<QueryType> {
            new CosmeticType("skin"),
            new CosmeticType("icon"),
            new CosmeticType("spray"),
            new CosmeticType("victorypose"),
            new CosmeticType("highlightintro"), 
            new CosmeticType("emote"),
            new CosmeticType("voiceline")
        };
        
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        public static readonly Dictionary<string, string> HeroMapping = new Dictionary<string, string> {
            ["soldier76"] = "soldier: 76",
            ["soldier 76"] = "soldier: 76",
            ["soldier"] = "soldier: 76",
            ["lucio"] = "lúcio",
            ["torbjorn"] = "torbjörn",
            ["torb"] = "torbjörn",
            ["toblerone"] = "torbjörn",
            ["dva"] = "d.va",
            ["fanservice"] = "d.va",
            ["starcraft_pro"] = "d.va",
            ["nano_cola_shill"] = "d.va",
            ["starcraft_pro_but_not_actually_because_michael_chu_retconned_it"] = "d.va",
            ["hammond"] = "wrecking ball",
            ["hamster"] = "wrecking ball",
            ["baguette"] = "brigitte",
            ["burrito"] = "brigitte"
        };

        public Dictionary<string, string> QueryNameOverrides => HeroMapping;

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            var heroes = GetHeroes();
            SaveUnlocksForHeroes(flags, heroes, basePath, NPCs);
        }

        public Task<Control> GetToolControl(ProgressWorker worker, SynchronizationContext context, Window window) {
            return WPF.Tool.Export.HeroUnlocksView.Get(worker, context, window, NPCs); 
        }

        public List<STUHero> GetHeroes() {
            var @return = new List<STUHero>();
            foreach (ulong key in TrackedFiles[0x75]) {
                var hero = GetInstance<STUHero>(key);
                // if (hero?.Name == null || hero.LootboxUnlocks == null) continue;

                @return.Add(hero);
            }

            return @return;
        }
        
        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();
            
            base.QueryHelp(types);
            
            Log("\r\nExample commands: ");
            Log($"{indent+1}\"Lúcio|skin=common\"");
            Log($"{indent+1}\"Torbjörn|skin=(rarity=legendary)\"");
            Log($"{indent+1}\"D.Va|skin=(event=summergames)\"");
            Log($"{indent+1}\"Soldier: 76|skin=Daredevil: 76\" \"Roadhog|spray=Pixel\"");
            Log($"{indent+1}\"Reaper|spray=*\" \t(extract all of Reaper's sprays)");
            Log($"{indent+1}\"Reaper|spray=(event=!halloween)\" \t\t(extract all of Reaper's sprays that are not from Halloween)");
            Log($"{indent+1}\"Reaper|skin=(rarity=legendary)\" \t\t(extract all of Reaper's legendary skins)");
            Log($"{indent+1}\"Reaper|spray=!Cute,*\" \t\t(extract all of Reaper's sprays except \"Cute\")");
            Log($"{indent+1}\"*|skin=(leagueteam=none)\" \t\t(extract skins for every hero ignoring Overwatch League skins)");
            
            // Log("https://www.youtube.com/watch?v=9Deg7VrpHbM");
        }

        private static Dictionary<teResourceGUID, string> EventConfig;

        public static IReadOnlyDictionary<teResourceGUID, string> GetEventConfig() {
            if (EventConfig != null) {
                return EventConfig;
            }

            var stu = OpenSTUSafe(TrackedFiles[0x54].First(x => teResourceGUID.Index(x) == 0x16C));
            var map = stu.GetInstance<STU_D7BD8322>();
            EventConfig = map.m_categories.ToDictionary(x => x.m_id.GUID, y => GetString(y.m_name));
            return EventConfig;
        }

        public void SaveUnlocksForHeroes(ICLIFlags flags, IEnumerable<STUHero> heroes, string basePath, bool npc=false) {
            if (flags.Positionals.Length < 4) {
                QueryHelp(QueryTypes);
                return;
            }

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;
            
            foreach (STUHero hero in heroes) {
                if (hero == null) continue;
                string heroNameActual = GetString(hero.m_0EDCE350);

                if (heroNameActual == null) {
                    continue;
                }

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*");
                
                heroNameActual = heroNameActual.TrimEnd(' ');
                string heroFileName = GetValidFilename(heroNameActual);
                
                if (config.Count == 0) continue;

                string heroPath = Path.Combine(basePath, RootDir, heroFileName);
                
                VoiceSet voiceSet = new VoiceSet(hero);
                ProgressionUnlocks progressionUnlocks = new ProgressionUnlocks(hero);
                if (progressionUnlocks.LevelUnlocks == null && !npc) {
                    continue;
                }
                if (progressionUnlocks.LootBoxesUnlocks != null && npc) {
                    continue;
                }
                
                Log($"Processing unlocks for {heroNameActual}");

                {
                    Combo.ComboInfo guiInfo = new Combo.ComboInfo();

                    foreach (STU_1A496D3C tex in hero.m_8203BFE1) {
                        Combo.Find(guiInfo, tex.m_texture);
                        guiInfo.SetTextureName(tex.m_texture, teResourceGUID.AsString(tex.m_id));
                    }

                    SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(heroPath, "GUI"), guiInfo);
                }

                if (progressionUnlocks.OtherUnlocks != null) { // achievements and stuff
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    SaveUnlocks(flags, progressionUnlocks.OtherUnlocks, heroPath, "Achievement", config, tags, voiceSet, hero);
                }
                
                if (npc) {
                    foreach (var skin in hero.m_skinThemes) {
                        if (!config.ContainsKey("skin") || !config["skin"].ShouldDo(GetFileName(skin.m_5E9665E3)))
                            continue;
                        SkinTheme.Save(flags, Path.Combine(heroPath, Unlock.GetTypeName(typeof(STUUnlock_SkinTheme)), 
                            string.Empty, GetFileName(skin.m_5E9665E3)), skin, hero);
                    }
                    continue;
                }

                if (progressionUnlocks.LevelUnlocks != null) { // default unlocks
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    foreach (LevelUnlocks levelUnlocks in progressionUnlocks.LevelUnlocks) {
                        SaveUnlocks(flags, levelUnlocks.Unlocks, heroPath, "Default", config, tags, voiceSet, hero);
                    }
                }

                if (progressionUnlocks.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in progressionUnlocks.LootBoxesUnlocks) {
                        if (lootBoxUnlocks.Unlocks == null) continue;
                        string lootboxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);
                        
                        var tags = new Dictionary<string, TagExpectedValue> {
                            {"event", new TagExpectedValue(LootBox.GetBasicName(lootBoxUnlocks.LootBoxType))}
                        };

                        SaveUnlocks(flags, lootBoxUnlocks.Unlocks, heroPath, lootboxName, config, tags, voiceSet, hero);
                    }
                }
            }
        }

        public static void SaveUnlocks(ICLIFlags flags, Unlock[] unlocks, string path, string eventKey,
            Dictionary<string, ParsedArg> config, Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet, STUHero hero) {
            if (unlocks == null) return;
            foreach (Unlock unlock in unlocks) {
                SaveUnlock(flags, unlock, path, eventKey, config, tags, voiceSet, hero);
            }
        }

        public static void SaveUnlock(ICLIFlags flags, Unlock unlock, string path, string eventKey,
            Dictionary<string, ParsedArg> config,
            Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet, STUHero hero) {
            string rarity;

            if (tags != null) {
                if (unlock.STU.m_0B1BA7C1 == null) {
                    rarity = unlock.Rarity.ToString();
                    tags["leagueTeam"] = new TagExpectedValue("none");
                } else {
                    TeamDefinition teamDef = new TeamDefinition(unlock.STU.m_0B1BA7C1);
                    tags["leagueTeam"] = new TagExpectedValue(teamDef.Abbreviation,  // NY
                        teamDef.Location,  // New York
                        teamDef.Name,  // Excelsior
                        teamDef.FullName,  // New York Excelsior
                        (teamDef.Division == Enum_5A789F71.None && teamDef.Location == null) ? "none" : "*",
                        "*");  // all
                
                    // nice file structure
                    rarity = "";
                    eventKey = "League";
                }
                tags["rarity"] = new TagExpectedValue(unlock.Rarity.ToString());

                tags["special"] = new TagExpectedValue(unlock.Tag ?? "none");
            } else {
                rarity = ""; // for general unlocks
            }

            var eventMap = GetEventConfig();
            if (unlock.STU.m_BEE9BCDA != null) {
                var formalEventKey = unlock.STU.m_BEE9BCDA.FirstOrDefault(x => eventMap.ContainsKey(x));
                if (eventMap.ContainsKey(formalEventKey)) {
                    eventKey = eventMap[formalEventKey];
                }
            }

            string thisPath = Path.Combine(path, unlock.GetTypeNameEnum(), eventKey, rarity, GetValidFilename(unlock.GetName()));
            
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_SprayPaint))) {
                SprayAndIcon.Save(flags, thisPath, unlock);
            }
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_AvatarPortrait))) {
                SprayAndIcon.Save(flags, thisPath, unlock);
            }
            
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_POTGAnimation))) {
                AnimationItem.Save(flags, thisPath, unlock);
            }
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_Emote))) {
                AnimationItem.Save(flags, thisPath, unlock);
            }
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_Pose))) {
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_VoiceLine))) {
                VoiceLine.Save(flags, thisPath, unlock, voiceSet);
            }
            
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_SkinTheme))) {
                SkinTheme.Save(flags, thisPath, unlock, hero);
            }
            
            if (ShouldDo(unlock, config, tags, typeof(STUUnlock_PortraitFrame))) {
                thisPath = Path.Combine(path, unlock.Type);
                PortraitFrame.Save(flags, thisPath, unlock);
            }
        }

        private static bool ShouldDo(Unlock unlock, Dictionary<string, ParsedArg> config,
            Dictionary<string, TagExpectedValue> tags, Type unlockType) {

            string type = Unlock.GetTypeName(unlockType);
            string typeLower = type.ToLowerInvariant();
            if (config == null) return unlock.Type == type;
            return unlock.Type == type && config.ContainsKey(typeLower) &&
                   config[typeLower].ShouldDo(unlock.GetName(), tags);
        }
    }
}
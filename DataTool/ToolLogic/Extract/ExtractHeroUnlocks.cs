using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic.Unlock;
using TankLib.STU.Types;
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
    
    [DebuggerDisplay("CosmeticType: {" + nameof(Name) + "}")]
    public class CosmeticType : QueryType {
        public CosmeticType(string name) {
            Name = name;
            Tags = new List<QueryTag> {
                new QueryTag("rarity", new List<string>{"common", "rare", "epic", "legendary"}),
                new QueryTag("event", new List<string>{"base", "summergames", "halloween", "winter", "lunarnewyear", "archives", "anniversary"}),
                new QueryTag("leagueTeam", new List<string>()),
                new QueryTag("special", new List<string> {"sg2018"})
            };
        }
    }
    
    [Tool("extract-unlocks", Description = "Extract hero cosmetics", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : QueryParser, ITool, IQueryParser {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

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
        
        public static Dictionary<string, string> HeroMapping = new Dictionary<string, string> {
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
            
            Log("Please specify what you want to extract:");
            Log($"{indent+1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent+1}Each query should be surrounded by \", and individual queries should be seperated by spaces");
            
            Log($"{indent+1}All hero and item names are in your selected locale");
                        
            Log("\r\nTypes:");
            foreach (QueryType argType in types) {
                Log($"{indent+1}{argType.Name}");
            }
            
            Log("\r\nTags:");

            foreach (QueryType argType in types) {
                foreach (QueryTag argTypeTag in argType.Tags) {
                    Log($"{indent+1}{argTypeTag.Name}:");
                    foreach (string option in argTypeTag.Options) {
                        Log($"{indent+2}{option}");
                    }
                }
                break;  // erm, ok
            }
            
            Log("\r\nExample commands: ");
            Log($"{indent+1}\"Lúcio|skin=common\"");
            Log($"{indent+1}\"Torbjörn|skin=(rarity=legendary)\"");
            Log($"{indent+1}\"D.Va|skin=(event=summergames)\"");
            Log($"{indent+1}\"Soldier: 76|skin=Daredevil: 76\" \"Roadhog|spray=Pixel\"");
            Log($"{indent+1}\"Reaper|spray=*\" \t(extract all of Reaper's sprays)");
            Log($"{indent+1}\"Reaper|spray=(event=!halloween)\" \t\t(extract all of Reper's sprays that are not from Halloween)");
            Log($"{indent+1}\"Reaper|skin=(rarity=legendary)\" \t\t(extract all of Reaper's legendary skins)");
            Log($"{indent+1}\"Reaper|spray=!Cute,*\" \t\t(extract all of Reaper's sprays except \"Cute\")");
            Log($"{indent+1}\"*|skin=(leagueteam=none)\" \t\t(extract skins for every hero ignoring Overwatch League skins)");
            
            // Log("https://www.youtube.com/watch?v=9Deg7VrpHbM");
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
                    Combo.Find(guiInfo, hero.m_D696F2F6);
                    guiInfo.SetTextureName(hero.m_D696F2F6, "Icon");
                    
                    Combo.Find(guiInfo, hero.m_D90B256D);
                    guiInfo.SetTextureName(hero.m_D90B256D, "Portrait");
                                        
                    Combo.Find(guiInfo, hero.m_EA6FF023);
                    guiInfo.SetTextureName(hero.m_EA6FF023, "Avatar");
                    
                    Combo.Find(guiInfo, hero.m_D3A31F29);
                    guiInfo.SetTextureName(hero.m_D3A31F29, "SpectatorIcon");
                    
                    Combo.Find(guiInfo, hero.m_DAD2E3A2);

                    SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(heroPath, "GUI"), guiInfo);
                }

                if (progressionUnlocks.OtherUnlocks != null) { // achievements and stuff
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    SaveUnlocks(flags, progressionUnlocks.OtherUnlocks, heroPath, "Achievement", config, tags, voiceSet, hero);
                }
                
                if (npc) {
                    foreach (var skin in hero.m_skinThemes) {
                        if (!config.ContainsKey("skin") && config["skin"].ShouldDo(GetFileName(skin.m_5E9665E3)))
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
                        "*");  // all
                
                    // nice file structure
                    rarity = "";
                    eventKey = "League";
                }
                tags["rarity"] = new TagExpectedValue(unlock.Rarity.ToString());
                
                //if (UnlockData.SummerGames2016.Contains(unlock.GUID)) {
                //    tags["special"] = new TagExpectedValue("sg2016");
                //} if (UnlockData.SummerGames2017.Contains(unlock.GUID)) {
                //    tags["special"] = new TagExpectedValue("sg2017");
                //} else 
                if (UnlockData.SummerGames2018.Contains(unlock.GUID)) {
                    tags["special"] = new TagExpectedValue("sg2018");
                } else {
                    tags["special"] = new TagExpectedValue("none");
                }
            } else {
                rarity = ""; // for general unlocks
            }
            
            string thisPath = Path.Combine(path, unlock.Type, eventKey, rarity, GetValidFilename(unlock.GetName()));
            
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic.Unlock;
using OWLib;
using STULib.Types.Generic;
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
                new QueryTag("rarity", new List<string>{"common", "rare", "legendary"}),
                new QueryTag("event", new List<string>{"base", "summergames", "halloween", "winter", "lunarnewyear", "uprising", "anniversary"}),
                new QueryTag("leagueTeam", new List<string>())
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
            ["dva"] = "d.va"
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
                var hero = GetInstanceNew<STUHero>(key);
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
            
            // Log("https://www.youtube.com/watch?v=9Deg7VrpHbM");
        }

        public void SaveUnlocksForHeroes(ICLIFlags flags, IEnumerable<STUHero> heroes, string basePath, bool npc=false) {
            if (flags.Positionals.Length < 4) {
                QueryHelp(QueryTypes);
                return;
            }
            
            Log("Initializing...");

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;
            
            foreach (STUHero hero in heroes) {
                if (hero == null) continue;
                string heroNameActual = GetString(hero.m_0EDCE350);

                if (heroNameActual == null) {
                    continue;
                    // heroFileName = "Unknown";
                    // heroNameActual = "Unknown";
                }

                Dictionary<string, ParsedArg> config = new Dictionary<string, ParsedArg>();
                foreach (string key in new [] {heroNameActual.ToLowerInvariant(), "*"}) {
                    if (!parsedTypes.ContainsKey(key)) continue;
                    foreach (KeyValuePair<string,ParsedArg> parsedArg in parsedTypes[key]) {
                        if (config.ContainsKey(parsedArg.Key)) {
                            config[parsedArg.Key] = config[parsedArg.Key].Combine(parsedArg.Value);
                        } else {
                            config[parsedArg.Key] = parsedArg.Value.Combine(null); // clone for safety
                        }
                    }
                }
                
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

                if (progressionUnlocks.OtherUnlocks != null) { // achievements and stuff
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    SaveUnlocks(flags, progressionUnlocks.OtherUnlocks, heroPath, "Achievement", config, tags, voiceSet);
                }

                if (progressionUnlocks.LevelUnlocks != null) { // default unlocks
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    foreach (LevelUnlocks levelUnlocks in progressionUnlocks.LevelUnlocks) {
                        SaveUnlocks(flags, levelUnlocks.Unlocks, heroPath, "Default", config, tags, voiceSet);
                    }
                }

                if (progressionUnlocks.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in progressionUnlocks.LootBoxesUnlocks) {
                        if (lootBoxUnlocks.Unlocks == null) continue;
                        string lootboxName;
                        if (!ItemEvents.GetInstance().EventsNormal.ContainsKey((uint)lootBoxUnlocks.LootBoxType)) {
                            lootboxName = $"Unknown{lootBoxUnlocks.LootBoxType}";
                        } else {
                            lootboxName = ItemEvents.GetInstance().EventsNormal[(uint)lootBoxUnlocks.LootBoxType];
                        }
                        
                        var tags = new Dictionary<string, TagExpectedValue> {
                            {"event", new TagExpectedValue(lootboxName.Replace(" ", "").ToLowerInvariant())}
                        };

                        SaveUnlocks(flags, lootBoxUnlocks.Unlocks, heroPath, lootboxName, config, tags, voiceSet);
                    }
                }

                /*var unlocks = GetInstanceNew<STUProgressionUnlocks>(hero.m_heroProgression);
                
                if (unlocks?.m_7846C401 == null && !npc)
                    continue;
                if (unlocks?.m_lootBoxesUnlocks != null && npc) {
                    continue;
                }
                
                Log($"Processing data for {heroNameActual}...");
                
                List<Unlock> weaponSkins = ListHeroUnlocks.GetUnlocksForHero(hero.m_heroProgression)?.SelectMany(x => x.Value.Where(y => y.Type == "Weapon")).ToList(); // eww?

                var achievementUnlocks = GatherUnlocks(unlocks?.m_otherUnlocks?.m_unlocks.Select(x => (ulong)x.GUID)).ToList();
                foreach (Unlock itemInfo in achievementUnlocks) {
                    if (itemInfo == null) continue;
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    SaveItemInfo(itemInfo, basePath, heroFileName, flags, hero, "Achievement", config, tags, weaponSkins);
                }

                if (npc) {
                    foreach (var skin in hero.m_skinThemes) {
                        if (config.ContainsKey("skin") && config["skin"].ShouldDo(GetFileName(skin.m_5E9665E3))) {
                            Skin.Save(flags, $"{basePath}\\{RootDir}", hero, skin);
                        }
                    }
                    continue;
                }

                foreach (var defaultUnlocks in unlocks.m_7846C401)  {
                    var dUnlocks = GatherUnlocks(defaultUnlocks.m_unlocks.m_unlocks.Select(it => (ulong) it)).ToList();
                    
                    foreach (Unlock itemInfo in dUnlocks) {
                        Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                        SaveItemInfo(itemInfo, basePath, heroFileName, flags, hero, "Standard", config, tags, weaponSkins);
                    }
                }

                foreach (var eventUnlocks in unlocks.m_lootBoxesUnlocks) {
                    if (eventUnlocks?.m_unlocks?.m_unlocks == null) continue;

                    string eventKey;
                    if (!ItemEvents.GetInstance().EventsNormal.ContainsKey((uint) eventUnlocks.m_lootboxType)) {
                        eventKey = $"Unknown{eventUnlocks.m_lootboxType}";
                    } else {
                        eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.m_lootboxType];
                    }
                    var eUnlocks = eventUnlocks.m_unlocks.m_unlocks.Select(it => GatherUnlock(it)).ToList();

                    foreach (Unlock itemInfo in eUnlocks) {
                        if (itemInfo == null) continue;
                        Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue(eventUnlocks.m_lootboxType.ToString().ToLowerInvariant())}};
                        SaveItemInfo(itemInfo, basePath, heroFileName, flags, hero, eventKey, config, tags, weaponSkins);
                    }
                }
                
                Combo.ComboInfo guiInfo = new Combo.ComboInfo();
                Combo.Find(guiInfo, hero.ImageResource1);
                Combo.Find(guiInfo, hero.ImageResource2);
                Combo.Find(guiInfo, hero.ImageResource3);
                Combo.Find(guiInfo, hero.ImageResource4);
                guiInfo.SetTextureName(hero.ImageResource1, "Icon");
                guiInfo.SetTextureName(hero.ImageResource2, "Portrait");
                guiInfo.SetTextureName(hero.ImageResource4, "Avatar");
                guiInfo.SetTextureName(hero.SpectatorIcon, "SpectatorIcon");
                SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, RootDir, heroFileName, "GUI"), guiInfo);*/
            }
        }

        public void SaveUnlocks(ICLIFlags flags, Unlock[] unlocks, string path, string eventKey,
            Dictionary<string, ParsedArg> config, Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet) {
            foreach (Unlock unlock in unlocks) {
                SaveUnlock(flags, unlock, path, eventKey, config, tags, voiceSet);
            }
        }

        public void SaveUnlock(ICLIFlags flags, Unlock unlock, string path, string eventKey,
            Dictionary<string, ParsedArg> config,
            Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet) {
            string rarity;

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
            
            string thisPath = Path.Combine(path, unlock.Type, eventKey, rarity, GetValidFilename(unlock.Name).Replace(".", ""));
            
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
        }

        private static bool ShouldDo(Unlock unlock, Dictionary<string, ParsedArg> config,
            Dictionary<string, TagExpectedValue> tags, Type unlockType) {

            string type = Unlock.GetTypeName(unlockType);
            string typeLower = type.ToLowerInvariant();
            return unlock.Type == type && config.ContainsKey(typeLower) && config[typeLower].ShouldDo(unlock.Name, tags);
        }

        /*public void SaveItemInfo(Unlock itemInfo, string basePath, string heroFileName, ICLIFlags flags, STUHero hero, 
            string eventKey, Dictionary<string, ParsedArg> config, Dictionary<string, TagExpectedValue> tags, List<Unlock> weaponSkins) {
            if (itemInfo?.STU == null) return;

            if (itemInfo.STU.LeagueTeam != null) {
                STULeagueTeam team = GetInstanceNew<STULeagueTeam>(itemInfo.STU.LeagueTeam);
                tags["leagueTeam"] = new TagExpectedValue(GetString(team.Abbreviation),  // NY
                    GetString(team.Location),  // New York
                    GetString(team.Name),  // Excelsior
                    $"{GetString(team.Location)} {GetString(team.Name)}",  // New York Excelsior
                    "*");  // all
                eventKey = "League";
                itemInfo.Rarity = "";
            } else {
                tags["leagueTeam"] = new TagExpectedValue("none");
            }
            
            if (eventKey == "Achievement" && itemInfo.Type == "Skin") itemInfo.Rarity = "";
            
            
            tags["rarity"] = new TagExpectedValue(itemInfo.Rarity);
            
            if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                SprayAndIcon.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
            }
            if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                SprayAndIcon.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
            }
            if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                Skin.Save(flags, $"{basePath}\\{RootDir}", hero, $"{eventKey}\\{itemInfo.Rarity}", itemInfo.STU as STUUnlock_Skin, weaponSkins);
            }
            if (itemInfo.Type == "Pose" && config.ContainsKey("victorypose") && config["victorypose"].ShouldDo(itemInfo.Name, tags)) {
                AnimationItem.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
            }
            if (itemInfo.Type == "HighlightIntro" && config.ContainsKey("highlightintro") && config["highlightintro"].ShouldDo(itemInfo.Name, tags)) {
                AnimationItem.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
            }
            if (itemInfo.Type == "Emote" && config.ContainsKey("emote") && config["emote"].ShouldDo(itemInfo.Name, tags)) {
                AnimationItem.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
            }
            if (itemInfo.Type == "VoiceLine" && config.ContainsKey("voiceline") && config["voiceline"].ShouldDo(itemInfo.Name, tags)) {
                VoiceLine.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo, hero);
            }
        }*/
    }
}
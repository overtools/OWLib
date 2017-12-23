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
using DataTool.ToolLogic.List;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using Texture = DataTool.FindLogic.Texture;

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
    
    [DebuggerDisplay("ArgType: {" + nameof(Name) + "}")]
    public class CosmeticType : QueryType {
        public CosmeticType(string name) {
            Name = name;
            Tags = new List<QueryTag> {
                new QueryTag("rarity", new List<string>{"common", "rare", "legendary"}),
                new QueryTag("event", new List<string>{"base", "summergames", "halloween", "winter", "lunarnewyear", "uprising", "anniversary"})
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
                string heroNameActual = GetString(hero.Name);
                string heroFileName = GetValidFilename(heroNameActual).TrimEnd(' ');

                if (heroFileName == null) {
                    continue;
                    // heroFileName = "Unknown";
                    // heroNameActual = "Unknown";
                }
                heroNameActual = heroNameActual.TrimEnd(' ');

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
                
                if (config.Count == 0) continue;
                
                var unlocks = GetInstance<STUHeroUnlocks>(hero.LootboxUnlocks);
                if (unlocks?.Unlocks == null && !npc)
                    continue;
                if (unlocks?.LootboxUnlocks != null && npc) {
                    continue;
                }
                
                Log($"Processing data for {heroNameActual}...");

                List<STULoadout> abilities = new List<STULoadout>();
                if (hero.Abilities != null) {
                    foreach (Common.STUGUID ability in hero.Abilities) {
                        STULoadout abilityInfo = GetInstance<STULoadout>(ability);
                        if (abilityInfo != null) abilities.Add(abilityInfo);
                    }
                }
                
                List<ItemInfo> weaponSkins = ListHeroUnlocks.GetUnlocksForHero(hero.LootboxUnlocks)?.SelectMany(x => x.Value.Where(y => y.Type == "Weapon")).ToList(); // eww?

                var achievementUnlocks = GatherUnlocks(unlocks?.SystemUnlocks?.Unlocks?.Select(it => (ulong)it)).ToList();
                foreach (ItemInfo itemInfo in achievementUnlocks) {
                    Dictionary<string, string> tags = new Dictionary<string, string> {{"event", "base"}, {"rarity", itemInfo.Rarity}};
                    if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                        SprayAndImage.SaveItem(basePath, heroFileName, RootDir, "Achievements", flags, itemInfo);
                    } else if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                        SprayAndImage.SaveItem(basePath, heroFileName, RootDir, "Achievements", flags, itemInfo);
                    } else if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                        Skin.Save(flags, $"{basePath}\\{RootDir}", hero, $"Achievement\\{itemInfo.Rarity}", itemInfo.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
                    } else if (itemInfo.Type == "Pose" && config.ContainsKey("victorypose") && config["victorypose"].ShouldDo(itemInfo.Name, tags)) {
                        AnimationItem.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                    } 
                    //else {
                    //    if (Debugger.IsAttached) Debugger.Break();
                    //}
                    // todo: add emote,highlightintro,victorypose whenever used
                }

                if (npc) {
                    foreach (STUHero.Skin skin in hero.Skins) {
                        Skin.Save(flags, $"{basePath}\\{RootDir}", hero, skin, false);
                    }
                    continue;
                }

                foreach (var defaultUnlocks in unlocks.Unlocks)  {
                    var dUnlocks = GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it)).ToList();
                    
                    foreach (ItemInfo itemInfo in dUnlocks) {
                        Dictionary<string, string> tags = new Dictionary<string, string> {{"event", "base"}, {"rarity", itemInfo.Rarity}};
                        if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                            SprayAndImage.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                            SprayAndImage.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                            Skin.Save(flags, $"{basePath}\\{RootDir}", hero, itemInfo.Rarity, itemInfo.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
                        }
                        if (itemInfo.Type == "Pose" && config.ContainsKey("victorypose") && config["victorypose"].ShouldDo(itemInfo.Name, tags)) {
                            AnimationItem.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "HighlightIntro" && config.ContainsKey("highlightintro") && config["highlightintro"].ShouldDo(itemInfo.Name, tags)) {
                            AnimationItem.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "Emote" && config.ContainsKey("emote") && config["emote"].ShouldDo(itemInfo.Name, tags)) {
                            AnimationItem.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "VoiceLine" && config.ContainsKey("voiceline") && config["voiceline"].ShouldDo(itemInfo.Name, tags)) {
                            VoiceLine.SaveItem(basePath, heroFileName, RootDir, "Standard", flags, itemInfo, hero);
                        }
                    }
                }

                foreach (var eventUnlocks in unlocks.LootboxUnlocks) {
                    if (eventUnlocks?.Unlocks?.Unlocks == null) continue;

                    var eventKey = ItemEvents.GetInstance().EventsNormal[(uint)eventUnlocks.Event];
                    var eUnlocks = eventUnlocks.Unlocks.Unlocks.Select(it => GatherUnlock(it)).ToList();

                    foreach (ItemInfo itemInfo in eUnlocks) {
                        Dictionary<string, string> tags = new Dictionary<string, string> {{"event", eventUnlocks.Event.ToString().ToLowerInvariant()}, {"rarity", itemInfo.Rarity.ToLowerInvariant()}};
                        if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                            SprayAndImage.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
                        }
                        if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                            SprayAndImage.SaveItem(basePath, heroFileName, RootDir, eventKey, flags, itemInfo);
                        }
                        if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                            Skin.Save(flags, $"{basePath}\\{RootDir}", hero, itemInfo.Rarity, itemInfo.Unlock as STULib.Types.STUUnlock.Skin, weaponSkins, abilities, false);
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
                    }
                }

                var heroTextures = new Dictionary<ulong, List<TextureInfo>>();
                heroTextures = Texture.FindTextures(heroTextures, hero.ImageResource1, "Icon", true);
                heroTextures = Texture.FindTextures(heroTextures, hero.ImageResource2, "Portrait", true);
                heroTextures = Texture.FindTextures(heroTextures, hero.ImageResource3, "unknown", true); // Same as Icon for now, doesn't get saved as its a dupe of icon
                // heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.SpectatorIcon, "Spectator Icon", true); // Also same as icon except has some transparency
                heroTextures = Texture.FindTextures(heroTextures, hero.ImageResource4, "Avatar", true);
                SaveLogic.Texture.Save(flags, Path.Combine(basePath, RootDir, heroFileName, "GUI"), heroTextures);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using OWLib;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.STUUnlock;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using Texture = DataTool.SaveLogic.Texture;

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
    
    // bases

    [DebuggerDisplay("ArgType: {" + nameof(Name) + "}")]
    public class ArgType {
        public string Name;
        public List<ArgTag> Tags;
    }

    [DebuggerDisplay("ArgTag: {" + nameof(Name) + "}")]
    public class ArgTag {
        public string Name;
        public List<string> Options;

        public ArgTag(string name, List<string> options) {
            Name = name;
            Options = options;
        }
    }

    // subs
    [DebuggerDisplay("ArgType: {" + nameof(Name) + "}")]
    public class CosmeticType : ArgType {
        public CosmeticType(string name) {
            Name = name;
            Tags = new List<ArgTag> {
                new ArgTag("rarity", new List<string>{"common", "rare", "legendary"}),
                new ArgTag("event", new List<string>{"base", "summergames", "halloween", "winter", "lunarnewyear", "uprising", "anniversary"})
            };
        }
    }

    public class ParsedArg {
        public string Type;
        public List<string> Allowed;
        public List<string> Disallowed;
        public Dictionary<string, string> Tags;


        public ParsedArg Combine(ParsedArg second) {
            if (second == null) return new ParsedArg { Type = Type, Allowed = Allowed, Disallowed = Disallowed, Tags = Tags};
            Dictionary<string, string> tagsNew = Tags;
            foreach (KeyValuePair<string,string> tag in second.Tags) {
                tagsNew[tag.Key] = tag.Value;
            }
            return new ParsedArg {Type = Type, Allowed = Allowed.Concat(second.Allowed).ToList(), 
                Disallowed = Disallowed.Concat(second.Disallowed).ToList(), Tags = tagsNew};
        }

        public bool ShouldDo(string name, Dictionary<string, string> tagVals=null) {
            if (tagVals != null) {
                foreach (KeyValuePair<string, string> tagVal in tagVals) {
                    if (!Tags.ContainsKey(tagVal.Key.ToLowerInvariant())) continue;
                    string tag = Tags[tagVal.Key.ToLowerInvariant()];
                    if (tag.StartsWith("!")) {
                        if (string.Equals(tag.Remove(0, 1), tagVal.Value, StringComparison.InvariantCultureIgnoreCase)) return false;
                    } else {
                        if (!string.Equals(tag, tagVal.Value, StringComparison.InvariantCultureIgnoreCase)) return false;
                    }
                }
            }
            string nameReal = name.ToLowerInvariant();
            return (Allowed.Contains(nameReal) || Allowed.Contains("*")) && (!Disallowed.Contains(nameReal) || !Disallowed.Contains("*"));
        }
    }
    
    [Tool("extract-unlocks", Description = "Extract all heroes sprays and icons", TrackTypes = new ushort[] { 0x75 }, CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        string rootDir = "Heroes";

        private readonly Dictionary<string, string> _HeroMapping = new Dictionary<string, string> {
            ["soldier76"] = "soldier: 76",
            ["soldier 76"] = "soldier: 76",
            ["soldier"] = "soldier: 76",
            ["lucio"] = "lúcio",
            ["torbjorn"] = "torbjörn"
        };

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            var heroes = GetHeroes();
            SaveUnlocksForHeroes(flags, heroes, basePath);
        }

        public List<STUHero> GetHeroes() {
            var @return = new List<STUHero>();
            foreach (ulong key in TrackedFiles[0x75]) {
                var hero = GetInstance<STUHero>(key);
                if (hero?.Name == null || hero.LootboxUnlocks == null) continue;

                @return.Add(hero);
            }

            return @return;
        }

        public class SubIndentHelper : IndentHelper {
            protected new static uint IndentStringPerLevel = 2;
        }
        
        public static void Help(List<ArgType> types) {
            IndentHelper indent = new IndentHelper();
            
            Log("Please specify what you want to extract:");
            Log($"{indent+1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent+1}Each query should be surrounded by \", and individual queries should be seperated by spaces");
                        
            Log("\r\nTypes:");
            foreach (ArgType argType in types) {
                Log($"{indent+1}{argType.Name}");
            }
            
            Log("\r\nTags:");

            foreach (ArgType argType in types) {
                foreach (ArgTag argTypeTag in argType.Tags) {
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

        public void SaveUnlocksForHeroes(ICLIFlags flags, List<STUHero> heroes, string basePath) {
            List<ArgType> types = new List<ArgType> {
                new CosmeticType("skin"),
                new CosmeticType("icon"),
                new CosmeticType("spray"),
                new CosmeticType("victorypose"),
                new CosmeticType("highlightintro"), 
                new CosmeticType("emote")
            };

            if (flags.Positionals.Length < 4) {
                Help(types);
                return;
            }

            string[] result = new string[flags.Positionals.Length-3];
            Array.Copy(flags.Positionals, 3, result, 0, flags.Positionals.Length-3);
            
            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes = new Dictionary<string, Dictionary<string, ParsedArg>>();
            
            foreach (string opt in result) {
                if (opt.StartsWith("--")) continue;  // ok so this is a flag
                string[] split = opt.Split('|');

                string hero = split[0].ToLowerInvariant();
                if (_HeroMapping.ContainsKey(hero)) {
                    hero = _HeroMapping[hero];
                }
                
                string[] afterOpts = new string[split.Length-1];
                Array.Copy(split, 1, afterOpts, 0, split.Length-1);
                
                parsedTypes[hero] = new Dictionary<string, ParsedArg>();

                if (afterOpts.Length == 0) {
                    foreach (ArgType type in types) {
                        parsedTypes[hero][type.Name] = new ParsedArg {Type = type.Name, Allowed = new List<string> {"*"}, Disallowed = new List<string>(), Tags = new Dictionary<string, string>()};
                    }
                    // everything for this hero
                } else {
                    foreach (string afterHeroOpt in afterOpts) {
                        string[] afterSplit = afterHeroOpt.Split('=');
                        
                        string type = afterSplit[0].ToLowerInvariant();
                        ArgType typeObj = types.FirstOrDefault(x => x.Name == type);
                        if (typeObj == null) {Log($"\r\nUnknown type: {type}\r\n"); Help(types); return;}
                        
                        parsedTypes[hero][typeObj.Name] = new ParsedArg {Type = typeObj.Name, Allowed = new List<string>(), Disallowed = new List<string>(), Tags = new Dictionary<string, string>()};
                        
                        string[] items = new string[afterSplit.Length - 1];
                        Array.Copy(afterSplit, 1, items, 0, afterSplit.Length - 1);
                        items = string.Join("=", items).Split(',');
                        bool isBracket = false;
                        foreach (string item in items) {
                            string realItem = item.ToLowerInvariant();
                            bool nextNotBracket = false;

                            if (item.StartsWith("(") && item.EndsWith(")")) {
                                realItem = item.Remove(0, 1);
                                realItem = realItem.Remove(realItem.Length-1);
                                isBracket = true;
                                nextNotBracket = true;
                            } else if (item.StartsWith("(")) {
                                isBracket = true;
                                realItem = item.Remove(0, 1);
                            } else if (item.EndsWith(")")) {
                                nextNotBracket = true;
                                realItem = item.Remove(item.Length-1);
                            }

                            if (!isBracket) {
                                if (!realItem.StartsWith("!")) {
                                    parsedTypes[hero][typeObj.Name].Allowed.Add(realItem);
                                } else {
                                    parsedTypes[hero][typeObj.Name].Disallowed.Add(realItem.Remove(0, 1));
                                }
                            } else {
                                string[] kv = realItem.Split('=');
                                string tagName = kv[0].ToLowerInvariant();
                                string tagValue = kv[1].ToLowerInvariant();
                                ArgTag tagObj = typeObj.Tags.FirstOrDefault(x => x.Name == tagName);
                                if (tagObj == null) {Log($"\r\nUnknown tag: {tagName}\r\n"); Help(types); return;}
                                
                                parsedTypes[hero][typeObj.Name].Tags[tagName] = tagValue;
                            }
                            if (nextNotBracket) isBracket = false;
                        }

                        if (parsedTypes[hero][typeObj.Name].Allowed.Count == 0 &&
                            parsedTypes[hero][typeObj.Name].Tags.Count > 0) {
                            parsedTypes[hero][typeObj.Name].Allowed = new List<string> {"*"};
                        }
                    }
                }
            }

            foreach (var hero in heroes) {
                var heroName = GetValidFilename(GetString(hero.Name));
                
                if (heroName == null) continue;
                
                Dictionary<string, ParsedArg> config = new Dictionary<string, ParsedArg>();
                foreach (string key in new [] {GetString(hero.Name).ToLowerInvariant(), "*"}) {
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
                if (unlocks?.Unlocks == null)
                    continue;

                List<STULoadout> abilities = new List<STULoadout>();
                foreach (Common.STUGUID ability in hero.Abilities) {
                    STULoadout abilityInfo = GetInstance<STULoadout>(ability);
                    if (abilityInfo != null) abilities.Add(abilityInfo);
                }
                
                List<ItemInfo> weaponSkins = List.ListHeroUnlocks.GetUnlocksForHero(hero.LootboxUnlocks, false).SelectMany(x => x.Value.Where(y => y.Type == "Weapon")).ToList(); // eww?

                var achievementUnlocks = GatherUnlocks(unlocks.SystemUnlocks?.Unlocks?.Select(it => (ulong)it)).ToList();
                foreach (ItemInfo itemInfo in achievementUnlocks) {
                    Dictionary<string, string> tags = new Dictionary<string, string> {{"event", "base"}, {"rarity", itemInfo.Rarity}};
                    if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                        SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, "Achievements", flags, itemInfo);
                    }
                    if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                        SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, "Achievements", flags, itemInfo);
                    }
                    if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                        SaveLogic.Unlock.Skin.Save(flags, basePath, hero, $"Achievement\\{itemInfo.Rarity}", itemInfo.Unlock as Skin, weaponSkins, abilities, false);
                    }
                    // todo: add emote,highlightintro,victorypose whenever used
                }

                foreach (var defaultUnlocks in unlocks.Unlocks)  {
                    var dUnlocks = GatherUnlocks(defaultUnlocks.Unlocks.Select(it => (ulong) it)).ToList();
                    
                    foreach (ItemInfo itemInfo in dUnlocks) {
                        Dictionary<string, string> tags = new Dictionary<string, string> {{"event", "base"}, {"rarity", itemInfo.Rarity}};
                        if (itemInfo.Type == "Spray" && config.ContainsKey("spray") && config["spray"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.Skin.Save(flags, basePath, hero, itemInfo.Rarity, itemInfo.Unlock as Skin, weaponSkins, abilities, false);
                        }
                        if (itemInfo.Type == "Pose" && config.ContainsKey("victorypose") && config["victorypose"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "HighlightIntro" && config.ContainsKey("highlightintro") && config["highlightintro"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "Emote" && config.ContainsKey("emote") && config["emote"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
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
                            SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, eventKey, flags, itemInfo);
                        }
                        if (itemInfo.Type == "PlayerIcon" && config.ContainsKey("icon") && config["icon"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.SprayAndImage.SaveItem(basePath, heroName, rootDir, eventKey, flags, itemInfo);
                        }
                        if (itemInfo.Type == "Skin" && config.ContainsKey("skin") && config["skin"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.Skin.Save(flags, basePath, hero, itemInfo.Rarity, itemInfo.Unlock as Skin, weaponSkins, abilities, false);
                        }
                        if (itemInfo.Type == "Pose" && config.ContainsKey("victorypose") && config["victorypose"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, "Standard", flags, itemInfo);
                        }
                        if (itemInfo.Type == "HighlightIntro" && config.ContainsKey("highlightintro") && config["highlightintro"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, eventKey, flags, itemInfo);
                        }
                        if (itemInfo.Type == "Emote" && config.ContainsKey("emote") && config["emote"].ShouldDo(itemInfo.Name, tags)) {
                            SaveLogic.Unlock.AnimationItem.SaveItem(basePath, heroName, rootDir, eventKey, flags, itemInfo);
                        }
                    }
                }

                var heroTextures = new Dictionary<ulong, List<TextureInfo>>();
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource1, "Icon", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource2, "Portrait", true);
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource3, "unknown", true); // Same as Icon for now, doesn't get saved as its a dupe
                heroTextures = FindLogic.Texture.FindTextures(heroTextures, hero.ImageResource4, "Avatar", true);
                Texture.Save(flags, Path.Combine(basePath, rootDir, heroName, "GUI"), heroTextures);
            }
        }
    }
}
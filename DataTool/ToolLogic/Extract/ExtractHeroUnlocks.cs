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
using DataTool.ToolLogic.Util;
using TankLib;
using TankLib.STU.Types;
using TankLib.STU.Types.Enums;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using static DataTool.Helper.SpellCheckUtils;
using Logger = TankLib.Helpers.Logger;

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
        public CosmeticType(UnlockType type, string humanName) : base(UnlockTypeToName(type)) {
            HumanName = humanName;
            Tags = new List<QueryTag> {
                new QueryTag("rarity", "Rarity", new List<string> {"common", "rare", "epic", "legendary"}),
                new QueryTag("event", "Event", new List<string> {"base", "summergames", "halloween", "winter", "lunarnewyear", "archives", "anniversary"}),
                new QueryTag("leagueTeam", "League Team", new List<string>(), "none") {
                    DynamicChoicesKey = UtilDynamicChoices.VALID_OWL_TEAMS
                }
            };
            DynamicChoicesKey = UtilDynamicChoices.GetUnlockKey(type);
        }

        public static string UnlockTypeToName(UnlockType type) {
            return type.ToString().ToLowerInvariant();
        }
    }

    [Tool("extract-unlocks", Name = "Hero Cosmetics", Description = "Extract hero cosmetics", CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroUnlocks : QueryParser, ITool, IQueryParser {
        protected virtual string RootDir => "Heroes";
        protected virtual bool NPCs => false;
        public Dictionary<string, string> QueryNameOverrides => null;
        public virtual string DynamicChoicesKey => UtilDynamicChoices.VALID_HERO_NAMES;
        private static bool HasSavedAnything = false;

        public List<QueryType> QueryTypes => new List<QueryType> {
            new CosmeticType(UnlockType.Skin, "Skin"),
            new CosmeticType(UnlockType.Icon, "Icon") {
                Aliases = ["playericon"]
            },
            new CosmeticType(UnlockType.Spray, "Spray"),
            new CosmeticType(UnlockType.VictoryPose, "Victory Pose") {
                Aliases = ["pose"]
            },
            new CosmeticType(UnlockType.HighlightIntro, "Highlight Intro") {
                Aliases = ["highlight", "intro"]
            },
            new CosmeticType(UnlockType.Emote, "Emote"),
            new CosmeticType(UnlockType.VoiceLine, "Voice Line"),
            new CosmeticType(UnlockType.WeaponSkin, "Weapon Skin") {
                Aliases = ["weaponvariant", "weapon"]
            },
            new CosmeticType(UnlockType.NameCard, "Name Card(HERO SPECIFIC ONLY, MOST ARE NOT. USE extract-general)"),
            new CosmeticType(UnlockType.WeaponCharm, "Weapon Charm(HERO SPECIFIC ONLY, MOST ARE NOT. USE extract-general)") {
                Aliases = ["charm"]
            },
            new CosmeticType(UnlockType.Souvenir, "Souvenir(HERO SPECIFIC ONLY, MOST ARE NOT. USE extract-general)")
        };

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();

            SaveUnlocksForHeroes(flags, flags.OutputPath);

            if (!HasSavedAnything) {
                Logger.Warn("Tool", $"No unlocks were extracted as nothing was found matching your query? Did you spell the hero or unlock incorrectly?");
                if (Client.CreateArgs.TextLanguage != "enUS") {
                    Logger.Error("Tool", $"Reminder! Your language is set to \"{Client.CreateArgs.TextLanguage}\".");
                    Logger.Warn("Tool", $"When extracting data in other languages, the names of the unlocks must be in the language you're extracting as they might differ from their English versions.");
                }

            }
        }

        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();

            base.QueryHelp(types);

            Log("\r\nExample commands: ");
            Log($"{indent + 1}\"Lúcio|skin=common\"");
            Log($"{indent + 1}\"Torbjörn|skin=(rarity=legendary)\"");
            Log($"{indent + 1}\"D.Va|skin=(event=summergames)\"");
            Log($"{indent + 1}\"Soldier: 76|skin=Daredevil: 76\" \"Roadhog|spray=Pixel\"");
            Log($"{indent + 1}\"Reaper|spray=*\" \t(extract all of Reaper's sprays)");
            Log($"{indent + 1}\"Reaper|spray=(event=!halloween)\" \t\t(extract all of Reaper's sprays that are not from Halloween)");
            Log($"{indent + 1}\"Reaper|skin=(rarity=legendary)\" \t\t(extract all of Reaper's legendary skins)");
            Log($"{indent + 1}\"Reaper|spray=!Cute,*\" \t\t(extract all of Reaper's sprays except \"Cute\")");
            Log($"{indent + 1}\"*|skin=(leagueteam=none)\" \t\t(extract skins for every hero ignoring Overwatch League skins)");

            // Log("https://www.youtube.com/watch?v=9Deg7VrpHbM");
        }

        private static Dictionary<teResourceGUID, string> EventConfig;

        public static IReadOnlyDictionary<teResourceGUID, string> GetEventConfig() {
            if (EventConfig != null) {
                return EventConfig;
            }

            using (var stu = OpenSTUSafe(TrackedFiles[0x54].First(x => teResourceGUID.Index(x) == 0x16C))) {
                var map = stu.GetInstance<STU_D7BD8322>();
                EventConfig = map.m_categories.ToDictionary(x => x.m_id.GUID, y => GetCleanString(y.m_name));
                return EventConfig;
            }
        }

        public void SaveUnlocksForHeroes(ICLIFlags flags, string basePath) {
            if (flags.Positionals.Length < 4) {
                QueryHelp(QueryTypes);
                return;
            }

            var heroes = Helpers.GetHeroes();
            var validNames = Helpers.GetHeroNamesMapping(heroes);
            var parsedTypes = ParseQuery(flags, QueryTypes, namesForThisLocale: validNames);
            if (parsedTypes == null) return;

            FillHeroSpellDict(SymSpell);
            SpellCheckQuery(parsedTypes,SymSpell);

            foreach (var (heroGuid, hero) in heroes) {
                var heroNameActual = hero.Name;
                if (heroNameActual == null) continue;

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*", teResourceGUID.Index(heroGuid).ToString("X"));

                string heroFileName = GetValidFilename(heroNameActual);

                if (config.Count == 0) continue;

                string heroPath = Path.Combine(basePath, RootDir, heroFileName);

                VoiceSet voiceSet = new VoiceSet(hero.STU);
                ProgressionUnlocks progressionUnlocks = new ProgressionUnlocks(hero.STU);
                if (progressionUnlocks.LevelUnlocks == null && !NPCs) {
                    continue;
                }

                if (progressionUnlocks.LootBoxesUnlocks != null && NPCs) {
                    continue;
                }

                Log($"Processing unlocks for {heroNameActual}");

                {
                    Combo.ComboInfo guiInfo = new Combo.ComboInfo();

                    hero.STU.m_8203BFE1 ??= Array.Empty<STU_1A496D3C>(); // todo: fix the gosh darn stu
                    foreach (STU_1A496D3C tex in hero.STU.m_8203BFE1) {
                        Combo.Find(guiInfo, tex.m_texture);
                        guiInfo.SetTextureName(tex.m_texture, teResourceGUID.AsString(tex.m_id));
                    }

                    var guiContext = new SaveLogic.Combo.SaveContext(guiInfo);
                    SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(heroPath, "GUI"), guiContext, new SaveLogic.Combo.SaveTextureOptions {
                        ProcessIcon = true
                    });
                }

                if (progressionUnlocks.OtherUnlocks != null) { // achievements and stuff
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    SaveUnlocks(flags, progressionUnlocks.OtherUnlocks, heroPath, "Achievement", config, tags, voiceSet, hero.STU);
                }

                if (NPCs) {
                    foreach (var skin in hero.STU.m_skinThemes) {
                        if (!config.ContainsKey("skin") || !config["skin"].ShouldDo(GetFileName(skin.m_5E9665E3)))
                            continue;

                        SkinTheme.Save(flags, Path.Combine(heroPath, UnlockType.Skin.ToString(), string.Empty, GetFileName(skin.m_5E9665E3)), skin, hero.STU);
                        HasSavedAnything = true;
                    }

                    continue;
                }

                if (progressionUnlocks.LevelUnlocks != null) { // default unlocks
                    Dictionary<string, TagExpectedValue> tags = new Dictionary<string, TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                    foreach (LevelUnlocks levelUnlocks in progressionUnlocks.LevelUnlocks) {
                        SaveUnlocks(flags, levelUnlocks.Unlocks, heroPath, "Default", config, tags, voiceSet, hero.STU);
                    }
                }

                if (progressionUnlocks.LootBoxesUnlocks != null) {
                    foreach (LootBoxUnlocks lootBoxUnlocks in progressionUnlocks.LootBoxesUnlocks) {
                        if (lootBoxUnlocks.Unlocks == null) continue;
                        string lootboxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);

                        var tags = new Dictionary<string, TagExpectedValue> {
                            {"event", new TagExpectedValue(LootBox.GetBasicName(lootBoxUnlocks.LootBoxType))}
                        };

                        SaveUnlocks(flags, lootBoxUnlocks.Unlocks, heroPath, lootboxName, config, tags, voiceSet, hero.STU);
                    }
                }

                SaveScratchDatabase();
            }
        }

        public static void SaveUnlocks(
            ICLIFlags flags, Unlock[] unlocks, string path, string eventKey,
            Dictionary<string, ParsedArg> config, Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet, STUHero hero) {
            if (unlocks == null) return;
            foreach (Unlock unlock in unlocks) {
                SaveUnlock(flags, unlock, path, eventKey, config, tags, voiceSet, hero);
            }
        }

        public static void SaveUnlock(
            ICLIFlags flags, Unlock unlock, string path, string eventKey,
            Dictionary<string, ParsedArg> config,
            Dictionary<string, TagExpectedValue> tags, VoiceSet voiceSet, STUHero hero) {
            string rarity;

            if (tags != null) {
                if (unlock.STU.m_0B1BA7C1 == null) {
                    rarity = unlock.Rarity.ToString();
                    tags["leagueTeam"] = new TagExpectedValue("none");
                } else {
                    TeamDefinition teamDef = new TeamDefinition(unlock.STU.m_0B1BA7C1);
                    tags["leagueTeam"] = new TagExpectedValue(teamDef.Abbreviation, // NY
                                                              teamDef.Location, // New York
                                                              teamDef.Name, // Excelsior
                                                              teamDef.FullName, // New York Excelsior
                                                              (teamDef.Division == Enum_5A789F71.None && teamDef.Location == null) ? "none" : "*",
                                                              "*"); // all

                    // nice file structure
                    rarity = "";
                    eventKey = "League";
                }

                tags["rarity"] = new TagExpectedValue(unlock.Rarity.ToString());
            } else {
                rarity = ""; // for general unlocks
            }

            var eventMap = GetEventConfig();
            if (unlock.STU.m_BEE9BCDA != null) {
                var formalEventKey = unlock.STU.m_BEE9BCDA.FirstOrDefault(x => eventMap.ContainsKey(x));
                if (eventMap.ContainsKey(formalEventKey)) {
                    eventKey = eventMap[formalEventKey] ?? eventKey;
                }
            }

            string thisPath = Path.Combine(path, unlock.Type.ToString(), eventKey ?? "Default", GetValidFilename(unlock.GetName()));

            if (ShouldDo(unlock, config, tags, UnlockType.Spray)) {
                LoudLog($"\tExtracting spray {unlock.Name}");
                SprayAndIcon.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.Icon)) {
                LoudLog($"\tExtracting icon {unlock.Name}");
                SprayAndIcon.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.HighlightIntro)) {
                LoudLog($"\tExtracting highlight intro {unlock.Name}");
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.Emote)) {
                LoudLog($"\tExtracting emote {unlock.Name}");
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.VictoryPose)) {
                LoudLog($"\tExtracting pose {unlock.Name}");
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.VoiceLine)) {
                LoudLog($"\tExtracting voice line {unlock.Name}");
                VoiceLine.Save(flags, thisPath, unlock, voiceSet);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.Skin)) {
                SkinTheme.Save(flags, thisPath, unlock, hero);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.PortraitFrame)) {
                // LoudLog($"\tExtracting level frame {unlock.Name}");
                thisPath = Path.Combine(path, unlock.Type.ToString());
                PortraitFrame.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.NameCard)) {
                LoudLog($"\tExtracting name card {unlock.Name}");
                NameCard.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.WeaponCharm)) {
                LoudLog($"\tExtracting charm {unlock.Name}");
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.Souvenir)) {
                LoudLog($"\tExtracting souvenir {unlock.Name}");
                AnimationItem.Save(flags, thisPath, unlock);
            }

            if (ShouldDo(unlock, config, tags, UnlockType.WeaponSkin)) {
                if (unlock.STU.m_rarity == STUUnlockRarity.Common) {
                    Logger.Debug("ExtractHeroUnlock", $"skipping common rarity weapon {unlock.Name}");
                    return;
                }
                
                LoudLog($"\tExtracting weapon skin {unlock.Name}");
                WeaponSkin.Save(flags, thisPath, unlock, hero);
            }
        }

        private static bool ShouldDo(Unlock unlock, Dictionary<string, ParsedArg> config, Dictionary<string, TagExpectedValue> tags, UnlockType unlockType) {
            if (unlock.Type != unlockType) return false;
            
            bool shouldDo;
            if (config == null) {
                shouldDo = true;
            } else {
                var typeLower = CosmeticType.UnlockTypeToName(unlockType);
                shouldDo = config.TryGetValue(typeLower, out var configForType) &&
                           configForType.ShouldDo(unlock.GetName(), tags);
            }

            if (shouldDo) {
                HasSavedAnything = true;
            }

            return shouldDo;
        }
    }
}
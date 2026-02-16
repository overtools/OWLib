#nullable enable
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
using Logger = TankLib.Helpers.Logger;

namespace DataTool.ToolLogic.Extract;
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
            new QueryTag("rarity", "Rarity", new List<string> {"common", "rare", "epic", "legendary", "mythic"}),
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
    public virtual string DynamicChoicesKey => UtilDynamicChoices.VALID_HERO_NAMES;

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
        new CosmeticType(UnlockType.NameCard, "Name Card. !! USE extract-name-cards INSTEAD") {
            Aliases = ["name-card"]
        },
        new CosmeticType(UnlockType.WeaponCharm, "Weapon Charm. !! USE extract-charms INSTEAD") {
            Aliases = ["charm"]
        },
        new CosmeticType(UnlockType.Souvenir, "Souvenir. !! USE extract-souvenirs INSTEAD")
    };

    public void Parse(ICLIFlags toolFlags) {
        var flags = (ExtractFlags) toolFlags;
        flags.EnsureOutputDirectory();

        SaveUnlocksForHeroes(flags, flags.OutputPath);
    }

    protected override void QueryHelp(List<QueryType> types) {
        IndentHelper indent = new IndentHelper();

        base.QueryHelp(types);

        Log("\r\nExample commands: ");
        Log($"{indent + 1}\"Lúcio|skin=Overwatch Classic\"");
        Log($"{indent + 1}\"Tracer|skin=Track and Field\"");
        Log($"{indent + 1}\"Reinhardt|emote=*\"");
        Log($"{indent + 1}\"Junker Queen|victorypose=*\"");
        Log($"{indent + 1}\"Torbjörn|highlightintro=*\"");
        Log($"{indent + 1}\"Reaper|weaponskin=Hard Light\"");
        Log($"{indent + 1}\"Reinhardt|weaponskin=Bound Demon\"");

        // Log("https://www.youtube.com/watch?v=9Deg7VrpHbM");
    }

    private static Dictionary<teResourceGUID, string?>? EventConfig;

    public static IReadOnlyDictionary<teResourceGUID, string?> GetEventConfig() {
        if (EventConfig != null) {
            return EventConfig;
        }

        using var stu = OpenSTUSafe(TrackedFiles[0x54].First(x => teResourceGUID.Index(x) == 0x16C));
        var map = stu?.GetInstance<STU_D7BD8322>();
        
        EventConfig = map?.m_categories?.ToDictionary(x => x.m_id.GUID, y => GetCleanString(y.m_name));
        return EventConfig ?? [];
    }

    public void SaveUnlocksForHeroes(ICLIFlags flags, string basePath) {
        if (flags.Positionals.Length < 4) {
            QueryHelp(QueryTypes);
            return;
        }

        var heroes = Helpers.GetHeroes();
        var parsedTypes = ParseQuery(flags, QueryTypes, localizedNameOverrides: Helpers.GetHeroNameLocaleOverrides(heroes));
        if (parsedTypes == null) return;

        foreach (var (heroGuid, hero) in heroes) {
            var heroNameActual = hero.Name;
            if (heroNameActual == null) continue;

            var config = GetQuery(parsedTypes, heroNameActual, "*", teResourceGUID.Index(heroGuid).ToString("X"));
            if (config.Count == 0) continue;
            
            string heroFileName = GetValidFilename(heroNameActual);
            string heroPath = Path.Combine(basePath, RootDir, heroFileName);

            var voiceSet = VoiceSet.Load(hero.STU);
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

                foreach (STU_1A496D3C tex in hero.STU.m_8203BFE1 ?? []) {
                    Combo.Find(guiInfo, tex.m_texture);
                    guiInfo.SetTextureName(tex.m_texture, teResourceGUID.AsString(tex.m_id));
                }

                var guiContext = new SaveLogic.Combo.SaveContext(guiInfo);
                SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(heroPath, "GUI"), guiContext, new SaveLogic.Combo.SaveTextureOptions {
                    ProcessIcon = true
                });
            }

            if (progressionUnlocks.OtherUnlocks != null) { // achievements and stuff
                IgnoreCaseDict<TagExpectedValue> tags = new IgnoreCaseDict<TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                SaveUnlocks(flags, progressionUnlocks.OtherUnlocks, heroPath, "Achievement", config, tags, voiceSet, hero.STU);
            }

            if (NPCs) {
                foreach (var skin in hero.STU.m_skinThemes) {
                    if (!config.ContainsKey("skin") || !config["skin"].ShouldDo(GetFileName(skin.m_5E9665E3)))
                        continue;

                    SkinTheme.SaveNpcSkin(flags, Path.Combine(heroPath, UnlockType.Skin.ToString(), string.Empty, GetFileName(skin.m_5E9665E3)), skin, hero.STU);
                }

                continue;
            }

            if (progressionUnlocks.LevelUnlocks != null) { // default unlocks
                IgnoreCaseDict<TagExpectedValue> tags = new IgnoreCaseDict<TagExpectedValue> {{"event", new TagExpectedValue("base")}};
                foreach (LevelUnlocks levelUnlocks in progressionUnlocks.LevelUnlocks) {
                    SaveUnlocks(flags, levelUnlocks.Unlocks, heroPath, "Default", config, tags, voiceSet, hero.STU);
                }
            }

            if (progressionUnlocks.LootBoxesUnlocks != null) {
                foreach (LootBoxUnlocks lootBoxUnlocks in progressionUnlocks.LootBoxesUnlocks) {
                    string lootboxName = LootBox.GetName(lootBoxUnlocks.LootBoxType);

                    var tags = new IgnoreCaseDict<TagExpectedValue> {
                        {"event", new TagExpectedValue(LootBox.GetBasicName(lootBoxUnlocks.LootBoxType))}
                    };

                    SaveUnlocks(flags, lootBoxUnlocks.Unlocks, heroPath, lootboxName, config, tags, voiceSet, hero.STU);
                }
            }

            SaveScratchDatabase();
        }
            
        LogUnknownQueries(parsedTypes);
    }

    public static void SaveUnlocks(
        ICLIFlags flags,
        Unlock[]? unlocks,
        string path,
        string? eventKey,
        IgnoreCaseDict<ParsedArg>? config,
        IgnoreCaseDict<TagExpectedValue>? tags,
        VoiceSet? voiceSet,
        STUHero? hero) {
        if (unlocks == null) return;
        foreach (Unlock unlock in unlocks) {
            SaveUnlock(flags, unlock, path, eventKey, config, tags, voiceSet, hero);
        }
    }

    public static void SaveUnlock(
        ICLIFlags flags, Unlock unlock, string path, string? eventKey,
        IgnoreCaseDict<ParsedArg>? config,
        IgnoreCaseDict<TagExpectedValue>? tags, VoiceSet? voiceSet, STUHero? hero) {
        string rarity;

        if (tags != null) {
            if (unlock.STU.m_0B1BA7C1 == null) {
                rarity = unlock.Rarity.ToString();
                tags["leagueTeam"] = new TagExpectedValue("none");
            } else {
                TeamDefinition teamDef = TeamDefinition.Load(unlock.STU.m_0B1BA7C1)!;
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
            if (eventMap.TryGetValue(formalEventKey, out var eventMapName)) {
                eventKey = eventMapName ?? eventKey;
            }
        }

        eventKey = GetValidFilename(eventKey); // "2026: Season 1"
        string thisPath = Path.Combine(path, unlock.Type.ToString(), eventKey ?? "Default", GetValidFilename(unlock.GetName()));

        if (ShouldDo(unlock, config, tags, UnlockType.Spray)) {
            Log($"\tExtracting spray {unlock.Name}");
            SprayAndIcon.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.Icon)) {
            Log($"\tExtracting icon {unlock.Name}");
            SprayAndIcon.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.HighlightIntro)) {
            Log($"\tExtracting highlight intro {unlock.Name}");
            AnimationItem.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.Emote)) {
            Log($"\tExtracting emote {unlock.Name}");
            AnimationItem.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.VictoryPose)) {
            Log($"\tExtracting pose {unlock.Name}");
            AnimationItem.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.VoiceLine)) {
            Log($"\tExtracting voice line {unlock.Name}");
            VoiceLine.Save(flags, thisPath, unlock, voiceSet);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.Skin)) {
            SkinTheme.Save(flags, thisPath, unlock, hero);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.PortraitFrame)) {
            // Log($"\tExtracting level frame {unlock.Name}");
            thisPath = Path.Combine(path, unlock.Type.ToString());
            PortraitFrame.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.NameCard)) {
            Log($"\tExtracting name card {unlock.Name}");
            NameCard.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.WeaponCharm)) {
            Log($"\tExtracting charm {unlock.Name}");
            AnimationItem.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.Souvenir)) {
            Log($"\tExtracting souvenir {unlock.Name}");
            AnimationItem.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.CompetitiveSignature)) {
            Log($"\tExtracting signature {unlock.Name}");
            CompSignature.Save(flags, thisPath, unlock);
        }

        if (ShouldDo(unlock, config, tags, UnlockType.WeaponSkin)) {
            if (unlock.STU.m_rarity == STUUnlockRarity.Common) {
                Logger.Debug("ExtractHeroUnlock", $"skipping common rarity weapon {unlock.Name}");
                return;
            }

            WeaponSkin.Save(flags, thisPath, unlock, hero);
        }
    }

    // todo: add previous zhCN name for ow1 skins: "守望先锋"
    // but for whatever reason, ow1 and ow2 skins on rcn+zhCN are all called "守望先锋" now
    // needs to be fixed first
    private static string[] OW1SkinAlternateNames = [
        "Overwatch 1", // old en
        "オーバーウォッチ 1", // old jp
        "오버워치 1", // old kr
        "《鬥陣特攻》",
        /* todo, */
        "Classic", // old rcn-en
        "Overwatch Classic" // new en
    ];
    private static string[] OW2SkinAlternateNames = [
        "Overwatch 2", // old en
        "オーバーウォッチ 2", // old jp
        "오버워치 2", // old kr
        "《鬥陣特攻2》",
        "守望先锋归来", // old rcn-cn
        "Valorous", // old rcn-en
        "Overwatch", // new en
    ];

    private static bool ShouldDo(Unlock unlock, IgnoreCaseDict<ParsedArg>? config, Dictionary<string, TagExpectedValue>? tags, UnlockType unlockType) {
        if (unlock.Type != unlockType) return false;
        
        if (config == null) {
            return true;
        }

        var typeLower = CosmeticType.UnlockTypeToName(unlockType);
        if (!config.TryGetValue(typeLower, out var configForType)) {
            return false;
        }
        
        // todo: if there are issues with dup names (cn, for now), maybe it could be a precise locale mapping using data instead
        ReadOnlySpan<string> alternateNames = unlock.GetSTU().m_name.GUID.GUID switch {
            0x0DE00000000024D4 => OW1SkinAlternateNames,
            0x0DE000000000CB5F => OW2SkinAlternateNames, // shared
            0x0DE0000000022DAB => OW2SkinAlternateNames, // echo, freja
            0x0DE00000000179D3 => OW2SkinAlternateNames, // lw
            0x0DE000000001B41C => OW2SkinAlternateNames, // mauga
            0x0DE000000001AE12 => OW2SkinAlternateNames, // illari
            0x0DE000000001CAC5 => OW2SkinAlternateNames, // venture
            0x0DE00000000204E3 => OW2SkinAlternateNames, // hazard
            0x0DE0000000020A28 => OW2SkinAlternateNames, // juno
            0x0DE000000002B1D8 => OW2SkinAlternateNames, // vendetta
            0x0DE0000000029E6A => OW2SkinAlternateNames, // anran
            0x0DE000000002A7CC => OW2SkinAlternateNames, // jetpack cat
            _ => []
        };
        
        var shouldDo = configForType.ShouldDo(unlock.GetName(), tags, alternateNames);
        if (shouldDo) {
            LogAlternateName(unlock, configForType, tags, alternateNames);
        }

        return shouldDo;
    }

    private static void LogAlternateName(Unlock unlock, ParsedArg configForType, Dictionary<string, TagExpectedValue>? tags, ReadOnlySpan<string> alternateNames) {
        if (alternateNames.Length < 0) {
            return;
        }
        
        var shouldDoWithNoAlternateNames = configForType.ShouldDo(unlock.GetName(), tags);
        if (shouldDoWithNoAlternateNames) {
            // didn't match via an alternate name
            // nothing to log
            return;
        }

        // todo: this can be a little weird because the alt names are not precise to the active locale
        // e,g "Classic" skins have been renamed to "守望先锋经典版"
        foreach (var alternateName in alternateNames) {
            var shouldDoForThisAltName = configForType.ShouldDo(unlock.GetName(), tags, [alternateName]);
            if (!shouldDoForThisAltName) continue;
            
            Logger.Warn("Query", $"The tool automatically edited your query - After 2026: Season 1, \"{alternateName}\" skins have been renamed to \"{unlock.GetName()}\". The extracted data will use the new name.");
            break;
        }
    }
}
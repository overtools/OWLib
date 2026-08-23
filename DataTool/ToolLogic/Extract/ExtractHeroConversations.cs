using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Util;
using Spectre.Console;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using SkinTheme = DataTool.SaveLogic.Unlock.SkinTheme;

namespace DataTool.ToolLogic.Extract;

[ExtractTool("extract-conversations", "extract-hero-convo", Description = "Extracts heroes voice conversations")]
public class ExtractHeroConversations : QueryParser, ITool, IQueryParser {
    public List<QueryType> QueryTypes => new List<QueryType>();
    public string DynamicChoicesKey => UtilDynamicChoices.VALID_HERO_NAMES;

    public void Parse(ICLIFlags toolFlags) {
        var flags = (ExtractFlags) toolFlags;
        flags.EnsureOutputDirectory();

        var path = Path.Combine(flags.OutputPath, Container);
        if (flags.VoiceGroupByLocale) {
            path = Path.Combine(path, Program.Client.CreateArgs.SpeechLanguage ?? "enUS");
        }

        var parsedTypes = ParseQuery(flags, QueryTypes, localizedNameOverrides: Helpers.GetHeroNameLocaleOverrides());
        if (parsedTypes == null) {
            Logger.Warn("No query specified, extracting all conversations for all heroes.");
        }

        Logger.Log("Generating voiceline mappings, this will take a moment...");
        GenerateVoicelineMapping();
        ProcessConversations(flags, path, parsedTypes);

        LogUnknownQueries(parsedTypes);
    }

    private const string Container = "HeroConvo";
    private readonly ConcurrentDictionary<ulong, bool> SeenVoiceSets = new();
    private static readonly ConcurrentDictionary<ulong, (string heroName, Combo.VoiceLineInstanceInfo voiceLineInstance)> VoicelineHeroMapping = new();

    private void ProcessConversations(ExtractFlags flags, string basePath, Dictionary<string, ParsedHero> parsedTypes) {
        foreach (var conversationGuid in Program.TrackedFiles[0xD0]) {
            var conversation = GetInstance<STUVoiceConversation>(conversationGuid);
            if (conversation == null) continue;

            var shouldProcessConvo = false;
            if (parsedTypes != null) {
                foreach (var voicelineGuid in conversation.m_90D76F17) {
                    if (voicelineGuid.m_E295B99C == null || !VoicelineHeroMapping.ContainsKey(voicelineGuid.m_E295B99C)) {
                        continue;
                    }

                    var (heroName, instance) = VoicelineHeroMapping[voicelineGuid.m_E295B99C];
                    if (string.IsNullOrEmpty(heroName)) continue;

                    var config = GetQuery(parsedTypes, heroName, "*");
                    if (config.Count >= 1) {
                        shouldProcessConvo = true;
                        break;
                    }
                }
            } else {
                shouldProcessConvo = true; // no query so just process everything
            }

            if (!shouldProcessConvo) {
                continue;
            }

            Logger.Info($"Extracting {teResourceGUID.AsString(conversationGuid)}");

            var newPath = basePath;
            if (flags.VoiceGroupByHero) {
                var primaryHero = conversation.m_90D76F17.Where(x => x.m_E295B99C != null && VoicelineHeroMapping.ContainsKey(x.m_E295B99C)).Select(x => VoicelineHeroMapping[x.m_E295B99C].heroName).FirstOrDefault(x => !string.IsNullOrEmpty(x));
                if (string.IsNullOrEmpty(primaryHero)) {
                    primaryHero = "Unknown";
                }

                newPath = Path.Combine(basePath, primaryHero);
            }

            var i = 0;
            foreach (var voicelineGuid in conversation.m_90D76F17) {
                if (voicelineGuid.m_E295B99C == null || !VoicelineHeroMapping.ContainsKey(voicelineGuid.m_E295B99C)) {
                    continue;
                }

                i++;
                var (heroName, instance) = VoicelineHeroMapping[voicelineGuid.m_E295B99C];

                if (instance.SoundFiles.Count == 0) {
                    Logger.Debug($"No sound files found for this voiceline instance {teResourceGUID.AsString(instance.GUIDx06F)}, trying to find them in the STUSound {teResourceGUID.AsString(instance.ExternalSound)}");
                    var stuSound = GetInstance<STUSound>(instance.ExternalSound);
                    foreach (var mSoundWemFile in stuSound?.m_C32C2195?.m_soundWEMFiles ?? []) {
                        if (ExtractHeroVoiceBetter.BadSoundFiles.Contains(mSoundWemFile)) {
                            continue;
                        }

                        instance.SoundFiles.Add(mSoundWemFile);
                    }
                }

                foreach (var soundFile in instance.SoundFiles) {
                    var soundFileGuid = teResourceGUID.AsString(soundFile);
                    var filename = $"{i}-{heroName ?? "Unknown"}-{soundFileGuid}";
                    var path = Path.Combine(newPath, teResourceGUID.AsString(conversationGuid));
                    SaveLogic.Combo.SaveVoiceLineInstance(flags, path, instance, filename);
                }
            }
        }
    }

    public void GenerateVoicelineMapping() {
        var heroesDict = Helpers.GetHeroes();
        var sortedHeroes = heroesDict.Values
            .OrderBy(x => x.GUID.GUID) // order by guid
            .ToArray();

        var heroes = sortedHeroes.Where(x => x.IsHero).ToArray();
        var npcs = sortedHeroes.Where(x => !x.IsHero).ToArray();

        AnsiConsole.Progress()
            .AutoRefresh(true)
            .AutoClear(true)
            .HideCompleted(true)
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn(), new RemainingTimeColumn())
            .Start(ctx => {
                var task = ctx.AddTask("Generating voiceline mapping", new ProgressTaskSettings {
                    AutoStart = true,
                    MaxValue = sortedHeroes.Length
                });

                GenerateVoiceLineMapping(heroes, task, ctx);
                GenerateVoiceLineMapping(npcs, task, ctx);
            });


        foreach (var guid in Program.TrackedFiles[0x5F]) {
            if (SeenVoiceSets.ContainsKey(guid)) {
                continue;
            }

            var voiceSet = GetInstance<STUVoiceSet>(guid);
            if (voiceSet == null) continue;

            var npcName = $"{IO.GetCleanString(voiceSet.m_269FC4E9)} {IO.GetCleanString(voiceSet.m_C0835C08)}".Trim();
            if (string.IsNullOrEmpty(npcName)) {
                npcName = IO.GetNullableGUIDName(guid) ?? $"Unknown{teResourceGUID.Index(guid):X}";
            }

            var info = new Combo.ComboInfo();
            FindVoicelinesInVoiceSet(guid, npcName, ref info);
        }
    }

    private void GenerateVoiceLineMapping(HeroVM[] heroes, ProgressTask task, ProgressContext ctx) {
        Parallel.ForEach(heroes, new ParallelOptions {
            MaxDegreeOfParallelism = IO.GetParallelismAmount(8)
        }, hero => {
            string heroName = IO.GetValidFilename(hero.Name ?? $"Unknown{teResourceGUID.Index(hero.GUID)}");
            var heroStu = hero.STU;

            var heroTask = ctx.AddTask($"Processing {heroName}", new ProgressTaskSettings {
                AutoStart = true,
                MaxValue = 1
            });

            Combo.ComboInfo baseInfo = null;
            var heroVoiceSetGuid = GetInstance<STUVoiceSetComponent>(heroStu.m_gameplayEntity)?.m_voiceDefinition;
            if (heroVoiceSetGuid != null) {
                SeenVoiceSets.TryAdd(heroVoiceSetGuid, true);
            }

            if (FindVoicelinesInVoiceSet(heroVoiceSetGuid, heroName, ref baseInfo)) {
                // todo: this is double-enumerating
                var skins = new ProgressionUnlocks(heroStu).GetUnlocksOfType(UnlockType.Skin);
                heroTask.MaxValue = skins.Count();

                foreach (var unlock in skins) {
                    heroTask.Increment(1);
                    if (unlock.STU is not STUUnlock_SkinTheme unlockSkinTheme) {
                        // todo: uuhhh.. this obviously looks like a race
                        // leaving and commenting so i'm 100% sure
                        Logger.Warn("Convo", "Return race??");
                        return;
                    }
                    if (unlockSkinTheme.m_0B1BA7C1 != 0) {
                        // skipping team uniform skins as a minor performance optimization
                        continue;
                    }

                    var skinThemeGUID = unlockSkinTheme.m_skinTheme;
                    var skinTheme = GetInstance<STUSkinBase>(skinThemeGUID);
                    if (skinTheme == null) {
                        Logger.Debug("Convo", $"Unable to load skin {skinThemeGUID}");
                        continue;
                    }

                    var replacements = SkinTheme.GetReplacements(skinThemeGUID);
                    foreach (var (_, newVoiceSetGuid) in replacements) {
                        if (teResourceGUID.Type(newVoiceSetGuid) != 0x5F) continue;
                        
                        SeenVoiceSets.TryAdd(newVoiceSetGuid, true);
                    }

                    // we are running this code even if the voice set is not replaced
                    // if i remember correctly, individual voice sounds can be replaced?
                    // but don't quote me (2026 zingy)
                    Combo.ComboInfo skinInfo = null;
                    FindVoicelinesInVoiceSet(heroVoiceSetGuid, heroName, ref skinInfo, baseInfo, replacements);
                }
            } else {
                Logger.Debug("Convo", $"Hero {heroName} has no voice set");
            }

            heroTask.StopTask();
            task.Increment(1);
            ctx.Refresh();
        });
    }

    private bool FindVoicelinesInVoiceSet(ulong? voiceSetGuid, string heroName, ref Combo.ComboInfo info, Combo.ComboInfo baseCombo = null, Dictionary<ulong, ulong> replacements = null) {
        if (voiceSetGuid == null) {
            return false;
        }

        info = new Combo.ComboInfo();
        Combo.Find(info, voiceSetGuid.Value, replacements);

        // if we're processing a skin, baseCombo is the combo from the hero, this remove duplicate check removes any sounds that belong to the base hero
        // this ensures you only get sounds unique to the skin when processing a skin
        if (baseCombo != null) {
            if (!Combo.RemoveDuplicateVoiceSetEntries(baseCombo, ref info, voiceSetGuid.Value, Combo.GetReplacement(voiceSetGuid.Value, replacements)))
                return false;
        }

        foreach (var voiceSet in info.m_voiceSets) {
            if (voiceSet.Value.VoiceLineInstances == null) continue;

            foreach (var voicelineInstanceInfo in voiceSet.Value.VoiceLineInstances) {
                foreach (var voiceLineInstance in voicelineInstanceInfo.Value) {
                    if (VoicelineHeroMapping.ContainsKey(voiceLineInstance.GUIDx06F)) continue;

                    VoicelineHeroMapping[voiceLineInstance.GUIDx06F] = (heroName, voiceLineInstance);
                }
            }
        }

        return true;
    }
}
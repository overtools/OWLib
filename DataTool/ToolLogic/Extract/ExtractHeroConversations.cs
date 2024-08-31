using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using SkinTheme = DataTool.SaveLogic.Unlock.SkinTheme;

namespace DataTool.ToolLogic.Extract {
    [ExtractTool("extract-conversations", "extract-hero-convo", Description = "Extracts heroes voice conversations")]
    public class ExtractHeroConversations : QueryParser, ITool {
        public Dictionary<string, string> QueryNameOverrides => null;
        public List<QueryType> QueryTypes => new List<QueryType>();

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();

            var path = Path.Combine(flags.OutputPath, Container);
            if (flags.VoiceGroupByLocale) {
                path = Path.Combine(path, Program.Client.CreateArgs.SpeechLanguage ?? "enUS");
            }

            var validHeroes = Helpers.GetHeroNamesMapping();
            var parsedTypes = ParseQuery(flags, QueryTypes, namesForThisLocale: validHeroes);
            if (parsedTypes == null) {
                Logger.Warn("No query specified, extracting all conversations for all heroes.");
            }

            Logger.Log("Generating voiceline mappings, this will take a moment...");
            GenerateVoicelineMapping();
            ProcessConversations(flags, path, parsedTypes);
        }

        private const string Container = "HeroConvo";
        private static readonly Dictionary<ulong, (string heroName, Combo.VoiceLineInstanceInfo voiceLineInstance)> VoicelineHeroMapping = new Dictionary<ulong, (string heroName, Combo.VoiceLineInstanceInfo voiceLineInstance)>();

        private void ProcessConversations(ExtractFlags flags, string basePath, Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes) {
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

                        var config = GetQuery(parsedTypes, heroName.ToLowerInvariant(), "*");
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

                    // todo: hammond could partake in a conversation where he just squeaks in response...
                    // meaning no voice sound files

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
            var seenVoiceSets = new HashSet<ulong>();

            var heroesDict = Helpers.GetHeroes();
            var heroes = heroesDict.Values
                .OrderBy(x => !x.IsHero) // sort by hero first
                .ThenBy(x => x.GUID.GUID) // then by GUID
                .ToArray();

            foreach (var hero in heroes) {
                var heroStu = hero.STU;

                string heroName = IO.GetValidFilename(hero.Name ?? $"Unknown{teResourceGUID.Index(hero.GUID)}");
                Logger.Info($"Generating mapping for {heroName}");

                Combo.ComboInfo baseInfo = default;
                var heroVoiceSetGuid = GetInstance<STUVoiceSetComponent>(heroStu.m_gameplayEntity)?.m_voiceDefinition;
                seenVoiceSets.Add(heroVoiceSetGuid);

                if (FindVoicelinesInVoiceSet(heroVoiceSetGuid, heroName, ref baseInfo)) {
                    var skins = new ProgressionUnlocks(heroStu).GetUnlocksOfType(UnlockType.Skin);
                    foreach (var unlock in skins) {
                        if (!(unlock.STU is STUUnlock_SkinTheme unlockSkinTheme)) return;
                        if (unlockSkinTheme.m_0B1BA7C1 != 0)
                            continue;

                        Combo.ComboInfo info = default;
                        var skinThemeGUID = unlockSkinTheme.m_skinTheme;
                        var skinTheme = GetInstance<STUSkinBase>(unlockSkinTheme.m_skinTheme);
                        if (skinTheme == null)
                            continue;

                        var replacements = SkinTheme.GetReplacements(skinThemeGUID);
                        foreach (var (_, newVoiceSetGuid) in replacements) {
                            seenVoiceSets.Add(newVoiceSetGuid);
                        }

                        FindVoicelinesInVoiceSet(heroVoiceSetGuid, heroName, ref info, baseInfo, replacements);
                    }
                }
            }

            foreach (var guid in Program.TrackedFiles[0x5F]) {
                if (seenVoiceSets.Contains(guid)) {
                    continue;
                }

                var voiceSet = GetInstance<STUVoiceSet>(guid);
                if (voiceSet == null) continue;

                var npcName = $"{IO.GetCleanString(voiceSet.m_269FC4E9)} {IO.GetCleanString(voiceSet.m_C0835C08)}".Trim();
                if (string.IsNullOrEmpty(npcName)) {
                    npcName = IO.GetNullableGUIDName(guid) ?? $"Unknown{teResourceGUID.Index(guid):X}";
                }

                Logger.Log($"Generating mapping for {npcName}");
                var info = new Combo.ComboInfo();
                FindVoicelinesInVoiceSet(guid, npcName, ref info);
            }
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
}

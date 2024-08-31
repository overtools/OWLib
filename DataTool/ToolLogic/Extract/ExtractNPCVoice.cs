using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using DataTool.SaveLogic.Unlock;
using TankLib;
using TankLib.Helpers;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-npc-voice", Description = "Extracts NPC voicelines.", CustomFlags = typeof(ExtractFlags))]
    public class ExtractNPCVoice : JSONTool, ITool {
        private const string Container = "NPCVoice";

        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();
            var outputPath = Path.Combine(flags.OutputPath, Container);

            flags.VoiceGroupBySkin = false;

            var heroes = Helpers.GetHeroes();
            var npcHeroVoiceSets = new Dictionary<ulong, string>();
            var heroVoiceSets = new HashSet<ulong>();

            foreach (var (key, hero) in heroes) {
                var voiceSet = GetInstance<STUVoiceSetComponent>(hero.STU?.m_gameplayEntity)?.m_voiceDefinition;
                if (voiceSet == 0) continue;

                if (hero.IsHero) {
                    heroVoiceSets.Add(voiceSet);
                    continue;
                }

                npcHeroVoiceSets.TryAdd(voiceSet, hero.Name);
            }

            foreach (var skinThemeGUID in Program.TrackedFiles[0xA6]) {
                foreach (var runtimeOverride in SkinTheme.GetReplacements(skinThemeGUID)) {
                    var overrideGUID = runtimeOverride.Value;

                    if (teResourceGUID.Type(overrideGUID) != 0x5F) continue;
                    // this skin overrides a voice set.
                    // its not an npc, dont need it
                    heroVoiceSets.Add(overrideGUID);
                }
            }

            foreach (var guid in Program.TrackedFiles[0x5F]) {
                if (heroVoiceSets.Contains(guid)) continue;

                var voiceSet = GetInstance<STUVoiceSet>(guid);
                if (voiceSet == null) continue;

                string npcName;
                if (npcHeroVoiceSets.TryGetValue(guid, out var heroName)) {
                    npcName = heroName;
                } else {
                    npcName = $"{GetCleanString(voiceSet.m_269FC4E9)} {GetCleanString(voiceSet.m_C0835C08)}".Trim();
                    if (string.IsNullOrEmpty(npcName)) {
                        npcName = GetCleanString(voiceSet.m_8A1F9462);
                    }
                    if (string.IsNullOrEmpty(npcName)) {
                        npcName = GetNullableGUIDName(guid);
                    }
                }

                if (string.IsNullOrEmpty(npcName)) {
                    continue;
                }

                var npcFileName = GetValidFilename(npcName);

                Logger.Log($"Processing NPC {npcName}");
                var info = new Combo.ComboInfo();
                ExtractHeroVoiceBetter.SaveVoiceSet(flags, outputPath, npcFileName, "Default", guid, ref info);
            }
        }
    }
}

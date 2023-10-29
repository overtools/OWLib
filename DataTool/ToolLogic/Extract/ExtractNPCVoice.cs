using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.JSON;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-npc-voice", Description = "Extracts NPC voicelines.", CustomFlags = typeof(ExtractFlags))]
    class ExtractNPCVoice : JSONTool, ITool {
        private const string Container = "NPCVoice";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = Path.Combine(flags.OutputPath, Container);
            } else {
                throw new Exception("no output path");
            }

            flags.VoiceGroupBySkin = false;

            var heroes = Helpers.GetHeroes();
            var npcVoiceSets = new Dictionary<ulong, string>();

            foreach (var (key, hero) in heroes) {
                if (hero.IsHero) {
                    continue;
                }

                var voiceSet = GetInstance<STUVoiceSetComponent>(hero.STU?.m_gameplayEntity)?.m_voiceDefinition;
                if (voiceSet > 0) {
                    npcVoiceSets.TryAdd(voiceSet, hero.Name);
                }
            }

            foreach (var guid in Program.TrackedFiles[0x5F]) {
                var voiceSet = GetInstance<STUVoiceSet>(guid);
                if (voiceSet == null) continue;

                string npcName;
                var isHero = false;
                if (npcVoiceSets.TryGetValue(guid, out var heroName)) {
                    npcName = heroName;
                    isHero = true;
                } else {
                    npcName = $"{GetCleanString(voiceSet.m_269FC4E9)} {GetCleanString(voiceSet.m_C0835C08)}".Trim();
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
                ExtractHeroVoiceBetter.SaveVoiceSet(flags, basePath, npcFileName, "Default", guid, ref info);
            }
        }
    }
}

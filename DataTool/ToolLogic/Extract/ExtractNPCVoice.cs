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
        private static readonly List<string> WhitelistedNPCs = new List<string> {"OR14-NS", "B73-NS"};

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = Path.Combine(flags.OutputPath, Container);
            } else {
                throw new Exception("no output path");
            }

            foreach (var guid in Program.TrackedFiles[0x5F]) {
                var voiceSet = GetInstance<STUVoiceSet>(guid);
                if (voiceSet == null) continue;

                var npcName = GetValidFilename(GetString(voiceSet.m_269FC4E9));
                if (npcName == null) continue;

                Logger.Log($"Processing NPC {npcName}");
                var info = new Combo.ComboInfo();
                var ignoreGroups = !WhitelistedNPCs.Contains(npcName);
                ExtractHeroVoiceBetter.SaveVoiceSet(flags, basePath, npcName, guid, ref info, ignoreGroups: ignoreGroups);
            }
        }
    }
}

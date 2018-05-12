using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class VoiceLine {
        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, DataModels.Unlock item, STUHero hero) {
            if (item == null) return;
            if (!(item.STU is STUUnlock_VoiceLine vl)) return;
            const string type = "VoiceLines";
            string name = GetValidFilename(item.Name).Replace(".", "");

            STUVoiceSetComponent soundSetComponentContainer = GetInstance<STUVoiceSetComponent>(hero.EntityMain);

            if (soundSetComponentContainer?.VoiceSet == null) {
                Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found");
                return;
            }
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, soundSetComponentContainer.VoiceSet);

            FindLogic.Combo.VoiceSetInfo voiceSetInfo = info.VoiceSets[soundSetComponentContainer.VoiceSet];
            
            List<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances = new List<FindLogic.Combo.VoiceLineInstanceInfo>();
            using (Stream vlStream = OpenFile(vl.EffectResource)) {
                using (Chunked vlChunk = new Chunked(vlStream)) {
                    foreach (SVCE svce in vlChunk.GetAllOfTypeFlat<SVCE>()) {
                        if (svce == null) continue;
                        if (voiceSetInfo.VoiceLineInstances.ContainsKey(svce.Data.VoiceStimulus)) {
                            voiceLineInstances.AddRange(voiceSetInfo.VoiceLineInstances[svce.Data.VoiceStimulus]);
                        }
                    }
                }
            }
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", type, folderName, name);
            Combo.SaveVoiceStimuli(flags, output, info, voiceLineInstances, false);
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.Flag;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.Statescript.Components;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class VoiceLine {
        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, ItemInfo item, STUHero hero) {
            if (item == null) return;
            if (!(item.Unlock is STULib.Types.STUUnlock.VoiceLine vl)) return;
            const string type = "VoiceLines";
            string name = GetValidFilename(item.Name).Replace(".", "");

            STUEntityVoiceMaster soundMasterContainer = GetInstance<STUEntityVoiceMaster>(hero.EntityMain);

            if (soundMasterContainer?.VoiceMaster == null) {
                Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: soundMaster not found");
                return;
            }
            
            FindLogic.Combo.ComboInfo info = new FindLogic.Combo.ComboInfo();
            FindLogic.Combo.Find(info, soundMasterContainer.VoiceMaster);

            FindLogic.Combo.VoiceMasterInfo voiceMasterInfo = info.VoiceMasters[soundMasterContainer.VoiceMaster];
            
            List<FindLogic.Combo.VoiceLineInstanceInfo> voiceLineInstances = new List<FindLogic.Combo.VoiceLineInstanceInfo>();
            using (Stream vlStream = OpenFile(vl.EffectResource)) {
                using (Chunked vlChunk = new Chunked(vlStream)) {
                    foreach (SVCE svce in vlChunk.GetAllOfTypeFlat<SVCE>()) {
                        if (svce == null) continue;
                        if (voiceMasterInfo.VoiceLineInstances.ContainsKey(svce.Data.VoiceStimulus)) {
                            voiceLineInstances.AddRange(voiceMasterInfo.VoiceLineInstances[svce.Data.VoiceStimulus]);
                        }
                    }
                }
            }
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", type, folderName, name);
            Combo.SaveVoiceStimuli(flags, output, info, voiceLineInstances, false);
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.FindLogic;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.Statescript.Components;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.SaveLogic.Unlock {
    public class VoiceLine {
        public static void SaveItem(string basePath, string heroName, string containerName, string folderName, ICLIFlags flags, ItemInfo item, STUHero hero) {
            const string type = "VoiceLines";
            string name = GetValidFilename(item.Name);

            STUStatescriptSoundMaster soundMasterContainer = GetInstance<STUStatescriptSoundMaster>(hero.StatescriptHeroComponent1);

            if (soundMasterContainer == null) {
                Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine", "[DataTool.SaveLogic.Unlock.VoiceLine]: soundMaster not found");
                return;
            }

            STUSoundMaster master = GetInstance<STUSoundMaster>(soundMasterContainer.SoundMaster);

            if (!(item.Unlock is STULib.Types.STUUnlock.VoiceLine vl)) return;
            
            List<STUVoiceLineInstance> lines;

            using (Stream vlStream = OpenFile(vl.EffectResource)) {
                using (Chunked vlChunk = new Chunked(vlStream)) {
                    SVCE svce = vlChunk.GetAllOfTypeFlat<SVCE>().FirstOrDefault();
                    if (svce == null) return;

                    lines = master.VoiceLineInstances.Where(x => x.SoundDataContainer.Group == svce.Data.GUIDx078).ToList();
                }
            }
            
            string output = Path.Combine(basePath, containerName, heroName ?? "", type, folderName, name.Replace(".", "_"));

            foreach (STUVoiceLineInstance voiceLineInstance in lines) {
                foreach (STUSoundWrapper wrapper in new [] {voiceLineInstance.SoundContainer.Sound1, 
                    voiceLineInstance.SoundContainer.Sound2, voiceLineInstance.SoundContainer.Sound3, 
                    voiceLineInstance.SoundContainer.Sound4}) {
                    if (wrapper != null) {
                        Sound.Save(flags, output, new Dictionary<ulong, List<SoundInfo>> {{0, new List<SoundInfo> {new SoundInfo {GUID = wrapper.SoundResource}}}}, false);
                    }
                }
            }
        }
    }
}
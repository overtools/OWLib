using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-maps", Description = "Extract maps", TrackTypes = new ushort[] {0x9F, 0x0BC}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractMaps : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetMaps(toolFlags);
        }

        public void GetMaps(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMap map = GetInstance<STUMap>(key);
                if (map == null) continue;
                
                string name = GetValidFilename(GetString(map.Name)) ?? $"Unknown{GUID.Index(key):X}";
                Dictionary<ulong, List<SoundInfo>> sounds = new Dictionary<ulong, List<SoundInfo>>();

                // if (map.Gamemodes != null) {
                //     foreach (Common.STUGUID gamemodeGUID in map.Gamemodes) {
                //         STUGamemode gamemode = GetInstance<STUGamemode>(gamemodeGUID);
                //     }
                // }

                // string test1 = GetFileName(map.GetDataKey(1));
                // string test2 = GetFileName(map.GetDataKey(2));
                // string test3 = GetFileName(map.GetDataKey(8));
                // string test4 = GetFileName(map.GetDataKey(0xB));
                // string test5 = GetFileName(map.GetDataKey(0x11));
                // string test6 = GetFileName(map.GetDataKey(0x10));
                // using (Stream oneStream = OpenFile(map.GetDataKey(0xB))) {
                //     Map mapOne = new Map(oneStream);
                // }

                string mapPath = Path.Combine(basePath, name, GUID.Index(key).ToString("X")) + Path.DirectorySeparatorChar;
                CreateDirectoryFromFile(mapPath);
                
                // todo: map files with STUs are different now

                if (map.SoundMasterResource != null) {
                    sounds = Sound.FindSounds(sounds, map.SoundMasterResource);
                }
                if (!flags.SkipAudio) {
                    SaveLogic.Sound.Save(toolFlags, Path.Combine(mapPath, "Sounds"), sounds);
                }
            }
        }
    }
}
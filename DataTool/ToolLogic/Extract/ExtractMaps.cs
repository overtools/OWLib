using System;
using DataTool.Flag;
using STULib.Types;
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

        public static void GetMaps(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            foreach (ulong key in TrackedFiles[0x9F]) {
                STUMap map = GetInstance<STUMap>(key);
                
                SaveLogic.Map.Save(flags, map, key, basePath);
            }
        }
    }
}
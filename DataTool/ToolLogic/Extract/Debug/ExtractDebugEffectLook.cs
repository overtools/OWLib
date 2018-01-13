using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using STULib.Types.Generic;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-effectlook", Description = "Extract effect looks (debug)", TrackTypes = new ushort[] {0xA8}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugEffectLook : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractEffectLooks(toolFlags);
        }

        public void ExtractEffectLooks(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugEffectLooks";
            
            foreach (ulong key in TrackedFiles[0xA8]) {
                // STUEffectLook look = GetInstance<STUEffectLook>(key);
                Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                Texture.FindTextures(textures, new Common.STUGUID(key), null, true);
                // SaveLogic.Texture.Save(flags, Path.Combine(basePath, container, GetFileName(key)), textures);
            }
        }
    }
}
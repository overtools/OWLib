using System;
using System.Collections.Generic;
using DataTool.FindLogic;
using DataTool.Flag;
using STULib.Types.Generic;
using static DataTool.Program;

namespace DataTool.ToolLogic.List.Debug {
    [Tool("list-debug-decal", Description = "List decals (debug)", TrackTypes = new ushort[] {0xA8}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListDebugDecal : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetDecals();
        }

        public void GetDecals() {
            Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();

            foreach (ulong key in TrackedFiles[0xA8]) {
                textures = Texture.FindTextures(textures, new Common.STUGUID(key));
            }
        }
    }
}
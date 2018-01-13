using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.STUUnlock;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using Texture = DataTool.FindLogic.Texture;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-animspray", Description = "Extract animated sprays (debug)", TrackTypes = new ushort[] {0xA5}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugAnimatedSpray : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractAnimatedSprays(toolFlags);
        }

        public void ExtractAnimatedSprays(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugAnimatedSprays";
            
            foreach (ulong key in TrackedFiles[0xA5]) {
                Spray spray = GetInstance<Spray>(key);
                if (spray == null) continue;

                Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();

                foreach (STUEffectReference sprayKey in new[] {spray.Effect2, spray.Effect}) {
                    using (Stream file = OpenFile(sprayKey.Effect)) {
                        using (Chunked chunked = new Chunked(file)) {
                            foreach (IChunk chunk in chunked.Chunks) {
                                if (chunk.GetType() != typeof(SSCE)) continue;
                                SSCE ssce = chunk as SSCE;
                                if (ssce == null) continue;
                                Texture.FindTextures(textures, new Common.STUGUID(ssce.Data.TextureDefinition), null, true);
                                Texture.FindTextures(textures, new Common.STUGUID(ssce.Data.Material), null, true);
                            }
                        }
                    }
                }
                string path = Path.Combine(basePath, container, GetValidFilename(GetString(spray.CosmeticName))).Replace(".", "").TrimEnd();
                CreateDirectoryFromFile(path + "\\dfsdfsd");
                // SaveLogic.Texture.Save(flags, path, textures);
            }
        }
    }
}
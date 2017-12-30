using System;
using System.IO;
using DataTool.Flag;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types.STUUnlock;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.List.Debug {
    [Tool("list-debug-few-particles", Description = "List highlight intros with 1 particle (debug)", TrackTypes = new ushort[] {0xA5}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListDebugFewParticles : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetFewParticles();
        }

        public void GetFewParticles() {
            foreach (ulong key in TrackedFiles[0xA5]) {
                HighlightIntro highlightIntro = GetInstance<HighlightIntro>(key);
                if (highlightIntro?.Animation == null) continue;
                using (Stream animStream = OpenFile(highlightIntro.Animation)) {
                    using (BinaryReader animReader = new BinaryReader(animStream)) {
                        animStream.Position = 0x18L;
                        ulong infokey = animReader.ReadUInt64();
                        if (infokey == 0) continue;
                        using (Stream chunkStream = OpenFile(infokey)) {
                            Chunked chunked = new Chunked(chunkStream);

                            if (chunked.GetAllOfTypeFlat<RPCE>().Length == 1) {
                                Console.Out.WriteLine(GetString(highlightIntro.CosmeticName));
                            }
                        }
                    }
                }
            }
        }
    }
}
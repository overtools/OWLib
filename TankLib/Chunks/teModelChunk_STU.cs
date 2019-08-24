using System.IO;
using System.Runtime.InteropServices;
using TankLib.STU;
using TankLib.STU.Types;
using TACTLib.Helpers;
using TankLib.Helpers;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MSTU: StructuredData for model definitions</summary>
    public class teModelChunk_STU : IChunk {
        public string ID => "MSTU";
        
        /// <summary>MSTU header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ModelSTUHeader {
            public long Offset;
            public long Size;
        }
        
        /// <summary>Header data</summary>
        public ModelSTUHeader Header;

        public STUModel StructuredData;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<ModelSTUHeader>();

                reader.BaseStream.Position = Header.Offset;

                using (SliceStream sliceStream = new SliceStream(input, Header.Offset, Header.Size))
                using (var stu = new teStructuredData(sliceStream))
                    StructuredData = stu.GetMainInstance<STUModel>();
            }
        }
    }
}
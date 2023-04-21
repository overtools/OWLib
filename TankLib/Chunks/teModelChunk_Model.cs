using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MDLC: Defines model</summary>
    public class teModelChunk_Model : IChunk {
        public string ID => "MDLC";
        public List<IChunk> SubChunks { get; set; }
        // todo: this might be wrong type. i think it is this because it is the last unknown except MSTU

        /// <summary>MDLC header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ModelHeader {
            public long m_0;
            public long MaterialOffset;
            public fixed float BoundingBox[16];

            public fixed byte Padding[20];

            public ushort MaterialCount; // 76->96
            public byte m_98;
            public byte m_99;
            public byte m_100;
            public ulong m_104;
        }

        /// <summary>Header data</summary>
        public ModelHeader Header;

        public ulong[] Materials;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<ModelHeader>();

                if (Header.MaterialOffset > 0) {
                    input.Position = Header.MaterialOffset;
                    Materials = reader.ReadArray<ulong>(Header.MaterialCount);
                }
            }
        }

        public ulong GetMaterial(ushort id) {
            if(Materials == null || id >= Materials.Length) {
                return 0;
            }
            return Materials[id];
        }
    }
}
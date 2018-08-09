using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank LightingManifest, file type 0BD</summary>
    public class teLightingManifest {
        public teLightingManifest(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader);
            }
        }

        public teLightingManifest(BinaryReader reader) {
            Read(reader);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeaderStruct {
            public uint Magic;
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;
            public int ChunkCount;  // todo: always more than one
            public uint Unknown5;
            public uint Unknown6; // todo: not uint
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Chunk {
            public uint Unknown1;
            public short ChunkID;
            public short Unknown2;
        }

        public HeaderStruct Header;
        public Chunk[] Chunks;

        private void Read(BinaryReader reader) {
            Header = reader.Read<HeaderStruct>();

            if (Header.ChunkCount > 0) {
                Chunks = reader.ReadArray<Chunk>(Header.ChunkCount);
            }
        }
    }
}
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.Chunk {
    public class HTLC : IChunk {
        public string Identifier => "HTLC"; // CLTH - Cloth
        public string RootIdentifier => "LDOM"; // MODL - Model

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeaderInfo {
            public ulong count;
            public long desc;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ClothDesc {
            public long _unk1;
            public long _unk2;
            public long _unk3;
            public long _unk4;
            public long _unk5;
            public long _unk6;
            public long _unk7;
            public long _unk8;
            public long _unk9;
            public fixed byte name[32];
            public uint unk1Count;
            public uint unk2Count;
            public uint unk3Count;
            public uint unk4Count;
            public uint unk5Count;
            public uint unk6Count;
            public uint unk7Count;
            public uint unk8Count;
            public uint unk9Count;
            public ulong unkA;
            public float unkB;
            public ulong unkD;
            public uint unkE;
            public ushort unkF;
            public ushort unk10;
            public OWRecord unk11;
            public ulong unk12;
            public OWRecord unk13;

            public unsafe string Name {
                get {
                    fixed (byte* n = name) {
                        return Encoding.ASCII.GetString(n, 32).Split('\0')[0];
                    }
                }
            }
        }

        private HeaderInfo header;
        public HeaderInfo Header => header;

        private ClothDesc[] descriptors;
        public ClothDesc[] Descriptors => descriptors;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<HeaderInfo>();

                if (header.count > 0) {
                    descriptors = new ClothDesc[header.count];
                    input.Position = header.desc;
                    for (ulong i = 0; i < header.count; ++i) {
                        descriptors[i] = reader.Read<ClothDesc>();
                    }
                }
            }
        }
    }
}

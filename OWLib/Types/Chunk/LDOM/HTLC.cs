using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.Chunk {
    public class HTLC : IChunk {
        public string Identifier => "HTLC"; // CLTH - Cloth
        public string RootIdentifier => "LDOM"; // MODL - Model

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HeaderInfo {
            public ulong descCount;
            public long descOffset;
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
            public uint driverNodeCount;  // debug ref = 0x69 / 105
            public uint threeNodeRelationCount;  // debug ref = 0xa8 / 168
            public uint driverChainCount;  // debug ref = 0xe / 14
            public uint twoNodeRelationCount;  // debug ref = 0xF7 / 247
            public uint fourNodeRelationCount;   // debug ref = 0xDB / 219
            public uint unk6Count;   // debug ref = 1
            public uint driverChainMaxLength;  // debug ref = 0xD / 13
            public uint ninthTableCount;  // debug ref = 0x6B / 107
            public uint unk9Count;  // debug ref = 2
            public ulong collisionModelCount;  // debug ref = 5
            public float unkB;
            public ulong boneIndicesTableOffset;  // debug ref = 0x1b0 / 432
            public uint unkE;
            public ushort unkF;
            public ushort unk10;
            public OWRecord unk11;
            public ulong unk12;
            public ulong unkPadding;
            public uint clothBonesUsed;
            // public OWRecord unk13;  // number of cloth bones used?

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
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, true)) {
                header = reader.Read<HeaderInfo>();

                if (header.descCount > 0) {
                    descriptors = new ClothDesc[header.descCount];
                    input.Position = header.descOffset;
                    for (ulong i = 0; i < header.descCount; ++i) {
                        descriptors[i] = reader.Read<ClothDesc>();
                    }
                }
            }
        }
    }
}

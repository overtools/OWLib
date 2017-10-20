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

        public class ClothNode {
            public float X;
            public float Y;
            public float Z;
            public short DiagonalParent;
            public short VertialParent;
            public short ChainNumber;

            public bool IsChild;

            public short Bone1;
            public short Bone2;
            public short Bone3;
            public short Bone4;
        }

        private HeaderInfo header;
        public HeaderInfo Header => header;

        public ClothNode[][] Nodes;

        private ClothDesc[] descriptors;
        public ClothDesc[] Descriptors => descriptors;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, true)) {
                header = reader.Read<HeaderInfo>();

                if (header.descCount > 0) {
                    descriptors = new ClothDesc[header.descCount];
                    input.Position = header.descOffset;
                    Nodes = new ClothNode[header.descCount][];
                    for (ulong i = 0; i < header.descCount; ++i) {
                        descriptors[i] = reader.Read<ClothDesc>();

                        if (descriptors[i]._unk1 == 4574069944263674675) {
                            // Nodes[i] = null;
                            continue;
                        }
                        Nodes[i] = new ClothNode[descriptors[i].driverNodeCount];
                        reader.BaseStream.Position = descriptors[i]._unk1;
                        for (int nodeIndex = 0; nodeIndex < descriptors[i].driverNodeCount; nodeIndex++) {
                            long nodeStart = reader.BaseStream.Position;
                            float x = reader.ReadSingle();
                            float y = reader.ReadSingle();
                            float z = reader.ReadSingle();
                            float what = reader.ReadSingle();  // zero
                            float x2 = reader.ReadSingle();
                            float y2 = reader.ReadSingle();
                            float z2 = reader.ReadSingle();
                            
                            reader.BaseStream.Position = nodeStart+0x15c;
                            short ind1 = reader.ReadInt16();
                            short ind2 = reader.ReadInt16();
                            short ind3 = reader.ReadInt16();
                            short ind4 = reader.ReadInt16();
                            
                            reader.BaseStream.Position = nodeStart+0x170;
                            float weight1 = reader.ReadSingle();
                            float weight2 = reader.ReadSingle();
                            float weight3 = reader.ReadSingle();
                            float weight4 = reader.ReadSingle();
                            
                            reader.BaseStream.Position = nodeStart+0x140;
                            float verticalParentStrength1 = reader.ReadSingle();
                            
                            reader.BaseStream.Position = nodeStart+0x148;
                            float verticalParentStrength2 = reader.ReadSingle();
                            
                            reader.BaseStream.Position = nodeStart+0x154;
                            float diagonalParentStrength = reader.ReadSingle();
                            
                            reader.BaseStream.Position = nodeStart + 0x14c;
                            short verticalParent = reader.ReadInt16();
                            
                            reader.BaseStream.Position = nodeStart + 0x158;
                            short diagonalParent = reader.ReadInt16();
                            
                            reader.BaseStream.Position = nodeStart + 0x150;
                            short chainNumber = reader.ReadInt16();
                            
                            reader.BaseStream.Position = nodeStart + 0x15a;
                            byte isChild = reader.ReadByte();
                            
                            reader.BaseStream.Position = nodeStart + 0x180;
                            
                            Nodes[i][nodeIndex] = new ClothNode {X = x, Y=y, Z=z, ChainNumber = chainNumber, DiagonalParent = diagonalParent, VertialParent = verticalParent, Bone1 = ind1, Bone2 = ind2, Bone3 = ind3, Bone4 = ind4, IsChild = isChild==1};
                        }
                    }
                }
            }
        }
    }
}

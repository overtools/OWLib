using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;

namespace OWLib.Types.Chunk.LDOM {
    // ReSharper disable once InconsistentNaming
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
            public long section1Offset;
            public long section2Offset;
            public long section3Offset;
            public long section4Offset;
            public long section5Offset;
            public long section6Offset;
            public long section7Offset;
            public long section8Offset;
            public long section9Offset;
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
            public long boneIndicesTableOffset;  // debug ref = 0x1b0 / 432
            public uint unkE;
            public ushort unkF;
            public ushort unk10;
            public OWRecord unk11;
            public ulong unk12;
            public ulong unkPadding;
            public uint clothBonesUsed;
            public uint unk;
            // public OWRecord unk13;  // number of cloth bones used?

            public string Name {
                get {
                    fixed (byte* n = name) {
                        return Encoding.ASCII.GetString(n, 32).Split('\0')[0];
                    }
                }
            }
        }

        [DebuggerDisplay("Bone: {Bone} Weight: {Weight}")]
        public class ClothNodeWeight : IEquatable<ClothNodeWeight> {
            public short Bone;
            public float Weight;

            public ClothNodeWeight(short bone, float weight) {
                Bone = bone;
                Weight = weight;
            }

            public bool Equals(ClothNodeWeight other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Bone == other.Bone && Weight.Equals(other.Weight);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ClothNodeWeight) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (Bone.GetHashCode() * 397) ^ Weight.GetHashCode();
                }
            }
        }

        [DebuggerDisplay("ClothNode: {" + nameof(ID) + "}")]
        public class ClothNode : IEquatable<ClothNode> {
            public int ID;
            public float X;
            public float Y;
            public float Z;
            public short DiagonalParent;
            public short VerticalParent;
            public short ChainNumber;
            public bool IsChild;

            public ClothNodeWeight[] Bones;

            public Matrix3x4 Matrix;

            public bool Equals(ClothNode other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return ID == other.ID && X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && 
                       DiagonalParent == other.DiagonalParent && VerticalParent == other.VerticalParent && 
                       ChainNumber == other.ChainNumber && IsChild == other.IsChild && Equals(Bones, other.Bones);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ClothNode) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = ID;
                    hashCode = (hashCode * 397) ^ X.GetHashCode();
                    hashCode = (hashCode * 397) ^ Y.GetHashCode();
                    hashCode = (hashCode * 397) ^ Z.GetHashCode();
                    hashCode = (hashCode * 397) ^ DiagonalParent.GetHashCode();
                    hashCode = (hashCode * 397) ^ VerticalParent.GetHashCode();
                    hashCode = (hashCode * 397) ^ ChainNumber.GetHashCode();
                    hashCode = (hashCode * 397) ^ IsChild.GetHashCode();
                    hashCode = (hashCode * 397) ^ (Bones != null ? Bones.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        public HeaderInfo Header { get; private set; }
        public ClothNode[][] Nodes { get; private set; }
        public ClothDesc[] Descriptors { get; private set; }
        public Dictionary<int, short>[] NodeBones { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, true)) {
                Header = reader.Read<HeaderInfo>();

                if (Header.descCount > 0) {
                    Descriptors = new ClothDesc[Header.descCount];
                    input.Position = Header.descOffset;
                    Nodes = new ClothNode[Header.descCount][];
                    NodeBones = new Dictionary<int, short>[Header.descCount];
                    for (ulong i = 0; i < Header.descCount; ++i) {
                        Descriptors[i] = reader.Read<ClothDesc>();
                        
                        long afterpos = reader.BaseStream.Position;
                        
                        if (Descriptors[i].section8Offset != 4574069944263674675) {
                            reader.BaseStream.Position = Descriptors[i].section8Offset;
                            NodeBones[i] = new Dictionary<int, short>();
                            for (int nodeIndex = 0; nodeIndex < Descriptors[i].driverNodeCount; nodeIndex++) {
                                NodeBones[i][nodeIndex] = reader.ReadInt16();
                            }
                        }
                        
                        Nodes[i] = new ClothNode[Descriptors[i].driverNodeCount];
                        reader.BaseStream.Position = Descriptors[i].section1Offset;
                        for (int nodeIndex = 0; nodeIndex < Descriptors[i].driverNodeCount; nodeIndex++) {
                            long nodeStart = reader.BaseStream.Position;
                            float x = reader.ReadSingle();
                            float y = reader.ReadSingle();
                            float z = reader.ReadSingle();
                            uint zero = reader.ReadUInt32();  // zero
                            float x2 = reader.ReadSingle();
                            float y2 = reader.ReadSingle();
                            float z2 = reader.ReadSingle();

                            if (zero != 0) {
                                throw new InvalidDataException($"HTLC: zero != 0 ({zero})");
                            }

                            if (Math.Abs(x - x2) > 0.01 || Math.Abs(y - y2) > 0.01 || Math.Abs(z - z2) > 0.01) {
                                throw new InvalidDataException($"HTLC: location is different: {x}:{x2} {y}:{y2} {z}:{z2}");
                            }
                            
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
                            
                            if (isChild == 1 && chainNumber == -1) {
                                throw new InvalidDataException("HTLC: node is child but not in a chain");
                            }

                            reader.BaseStream.Position = nodeStart + 0xC0+16;
                            Matrix3x4 ff = reader.Read<Matrix3x4B>().ToOpenTK(); // this is wrong...
                            
                            reader.BaseStream.Position = nodeStart + 0x180;
                            
                            Nodes[i][nodeIndex] = new ClothNode {ID=nodeIndex, X = x, Y=y, Z=z, 
                                ChainNumber = chainNumber, DiagonalParent = diagonalParent, 
                                VerticalParent = verticalParent, Bones = new [] {new ClothNodeWeight(ind1, weight1), 
                                    new ClothNodeWeight(ind2, weight2), new ClothNodeWeight(ind3, weight3), 
                                    new ClothNodeWeight(ind4, weight4)}, IsChild = isChild==1, Matrix = ff
                            };
                        }
                        reader.BaseStream.Position = afterpos;
                    }
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>mskl: Defines model skeleton</summary>
    public class teModelChunk_Cloth : IChunk {
        public string ID => "CLTH";
        
        /// <summary>CLTH header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ClothHeader {
            public ulong Count;
            public long Offset;
        }
        
        /// <summary>Cloth description</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ClothDesc {
            public long Section1Offset;
            public long Section2Offset;
            public long Section3Offset;
            public long Section4Offset;
            public long Section5Offset;
            public long Section6Offset;
            public long Section7Offset;
            public long Section8Offset;
            public long Section9Offset;
            public fixed byte Name[32];
            public uint DriverNodeCount;  // debug ref = 0x69 / 105
            public uint ThreeNodeRelationCount;  // debug ref = 0xa8 / 168
            public uint DriverChainCount;  // debug ref = 0xe / 14
            public uint TwoNodeRelationCount;  // debug ref = 0xF7 / 247
            public uint FourNodeRelationCount;   // debug ref = 0xDB / 219
            public uint Unk6Count;   // debug ref = 1
            public uint DriverChainMaxLength;  // debug ref = 0xD / 13
            public uint NinthTableCount;  // debug ref = 0x6B / 107
            public uint Unk9Count;  // debug ref = 2
            public ulong CollisionModelCount;  // debug ref = 5
            public float UnkB;
            public long BoneIndicesTableOffset;  // debug ref = 0x1b0 / 432
            public uint UnkE;
            public ushort UnkF;
            public ushort Unk10;
            public ulong Unk11Padding;
            public teResourceGUID Unk11;
            public teResourceGUID Unk12;
            public ulong UnkPadding;
            public int ClothBonesUsed;
            public int Unk;
            // public OWRecord unk13;  // number of cloth bones used?

            public string StringName {
                get {
                    fixed (byte* n = Name) {
                        return Encoding.ASCII.GetString(n, 32).Split('\0')[0];
                    }
                }
            }
        }
        
        public class ClothNodeWeight {
            /// <summary>Bone index</summary>
            public short Bone;
            
            /// <summary>Weight value</summary>
            public float Weight;

            public ClothNodeWeight(short bone, float weight) {
                Bone = bone;
                Weight = weight;
            }
        }

        /// <summary>Cloth node</summary>
        public class ClothNode {
            public int ID;
            
            public float X;
            public float Y;
            public float Z;
            
            public short DiagonalParent;
            public short VerticalParent;

            public float DiagonalParentStrength;
            public float VerticalParentStrength1;
            public float VerticalParentStrength2;
            
            public short ChainNumber;
            public bool IsChild;

            public ClothNodeWeight[] Bones;
            
            //public Matrix3x4 Matrix;

            public ClothNode(BinaryReader reader) {
                long nodeStart = reader.BaseStream.Position;
                X = reader.ReadSingle();
                Y = reader.ReadSingle();
                Z = reader.ReadSingle();
                uint zero = reader.ReadUInt32();  // zero
                float x2 = reader.ReadSingle();
                float y2 = reader.ReadSingle();
                float z2 = reader.ReadSingle();
    
                if (zero != 0) {
                    throw new InvalidDataException($"HTLC: zero != 0 ({zero})");
                }
    
                if (System.Math.Abs(X - x2) > 0.01 || System.Math.Abs(Y - y2) > 0.01 || System.Math.Abs(Z - z2) > 0.01) {
                    throw new InvalidDataException($"HTLC: location is different: {X}:{x2} {Y}:{y2} {Z}:{z2}");
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

                Bones = new[] {
                    new ClothNodeWeight(ind1, weight1),
                    new ClothNodeWeight(ind2, weight2),
                    new ClothNodeWeight(ind3, weight3),
                    new ClothNodeWeight(ind4, weight4)
                };
                
                reader.BaseStream.Position = nodeStart+0x140;
                VerticalParentStrength1 = reader.ReadSingle();
                
                reader.BaseStream.Position = nodeStart+0x148;
                VerticalParentStrength2 = reader.ReadSingle();
                
                reader.BaseStream.Position = nodeStart+0x154;
                DiagonalParentStrength = reader.ReadSingle();
                
                reader.BaseStream.Position = nodeStart + 0x14c;
                VerticalParent = reader.ReadInt16();
                
                reader.BaseStream.Position = nodeStart + 0x158;
                DiagonalParent = reader.ReadInt16();
                
                reader.BaseStream.Position = nodeStart + 0x150;
                ChainNumber = reader.ReadInt16();
                
                reader.BaseStream.Position = nodeStart + 0x15a;
                IsChild = reader.ReadBoolean();
                
                if (IsChild && ChainNumber == -1) {
                    throw new InvalidDataException("HTLC: node is child but not in a chain");
                }
    
                //reader.BaseStream.Position = nodeStart + 0xC0+16;
                //Matrix3x4 ff = reader.Read<Matrix3x4B>().ToOpenTK(); // this is wrong...
                
                reader.BaseStream.Position = nodeStart + 0x180;
            }
        }
        
        /// <summary>Header data</summary>
        public ClothHeader Header;
        
        /// <summary>Cloth descriptors</summary>
        public ClothDesc[] Descriptors;
        
        /// <summary>Cloth nodes</summary>
        public ClothNode[][] Nodes;
        
        /// <summary>Cloth nodes => skeleton bones map</summary>
        public Dictionary<int, short>[] NodeBones;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<ClothHeader>();

                if (Header.Offset > 0) {
                    reader.BaseStream.Position = Header.Offset;
                    
                    Descriptors = new ClothDesc[Header.Count];
                    Nodes = new ClothNode[Header.Count][];
                    NodeBones = new Dictionary<int, short>[Header.Count];

                    for (ulong i = 0; i < Header.Count; i++) {
                        Descriptors[i] = reader.Read<ClothDesc>();
                        
                        long nextStartPos = reader.BaseStream.Position;
                        
                        if (Descriptors[i].Section8Offset != 4574069944263674675) {  // todo: wtf
                            reader.BaseStream.Position = Descriptors[i].Section8Offset;
                            NodeBones[i] = new Dictionary<int, short>();
                            for (int nodeIndex = 0; nodeIndex < Descriptors[i].DriverNodeCount; nodeIndex++) {
                                NodeBones[i][nodeIndex] = reader.ReadInt16();
                            }
                        }

                        if (Descriptors[i].Section1Offset > 0 && Descriptors[i].Section1Offset != 4692750811720056850) {  // todo: wtf2
                            Nodes[i] = new ClothNode[Descriptors[i].DriverNodeCount];
                            reader.BaseStream.Position = Descriptors[i].Section1Offset;
                            for (int nodeIndex = 0; nodeIndex < Descriptors[i].DriverNodeCount; nodeIndex++) {
                                Nodes[i][nodeIndex] = new ClothNode(reader);
                            } 
                        }

                        reader.BaseStream.Position = nextStartPos;
                    }
                }
            }
        }

        public short[] CreateFakeHierarchy(teModelChunk_Skeleton skeleton, out Dictionary<int, ClothNode> nodeMap) {
            short[] hierarchy = (short[]) skeleton.Hierarchy.Clone();
            nodeMap = new Dictionary<int, ClothNode>();
            
            uint clothIndex = 0;
            foreach (ClothNode[] nodeCollection in Nodes) {
                if (nodeCollection == null) continue;
                int nodeIndex = 0;
                foreach (ClothNode node in nodeCollection) {
                    int parentRaw = node.VerticalParent;
                    if (NodeBones[clothIndex].ContainsKey(nodeIndex) &&
                        NodeBones[clothIndex].ContainsKey(parentRaw)) {
                        if (NodeBones[clothIndex][nodeIndex] != -1) {
                            hierarchy[NodeBones[clothIndex][nodeIndex]] =
                                NodeBones[clothIndex][parentRaw];
                            if (NodeBones[clothIndex][parentRaw] == -1) {
                                ClothNodeWeight weightedBone =
                                    node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                hierarchy[NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                            }
                        }
                    } else {
                        if (NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                            if (NodeBones[clothIndex][nodeIndex] != -1) {
                                hierarchy[NodeBones[clothIndex][nodeIndex]] = -1;
                                ClothNodeWeight weightedBone =
                                    node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                hierarchy[NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                            }
                        }
                    }
                    if (NodeBones[clothIndex].ContainsKey(nodeIndex)) {
                        if (NodeBones[clothIndex][nodeIndex] != -1) {
                            nodeMap[NodeBones[clothIndex][nodeIndex]] = node;
                        }
                    }

                    nodeIndex++;
                }
                clothIndex++;
            }

            return hierarchy;
        }
    }
}
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>mskl: Defines model skeleton</summary>
    public class teModelChunk_Cloth : IChunk {
        public string ID => "CLTH";
        public List<IChunk> SubChunks { get; set; }

        /// <summary>CLTH header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ClothHeader {
            public ulong Count;
            public long Offset;
        }

        /// <summary>Cloth description</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct ClothDesc {
            public long PositionsOffset;
            public long SectionOffset3;
            public long SectionOffset4;
            public long SectionOffset5;
            public long SectionOffset6;
            public long SectionOffset7;
            public long SectionOffset8;
            public long VertParent1Offset;
            public long VertParent2Offset;
            public long DiagParentOffset;
            public long WeightOffset;
            public long IndicesOffset;
            public long BoneSectionOffset;
            public long SectionOffset15;
            public long SectionOffset16;
            public long SectionOffset17;
            public long SectionOffset18;
            public long SectionOffset19;
            public long SectionOffset20;
            public long SectionOffset21;
            public long SectionOffset22;
            public long SectionOffset23;
            public long SectionOffset24;
            public long SectionOffset25;
            public long SectionOffset26;
            public long SectionOffset27;
            public fixed byte Name[32];
            public int Unknown1;
            public ushort ClothBonesUsed;
            public ushort DriverNodeCount;
            public ushort UnkCount3;
            public ushort ThreeNodeRelationCount;
            public ushort UnkCount5;
            public ushort UnkCount6;
            public ushort UnkCount7;
            public ushort UnkCount8;
            public ushort UnkCount9;
            public ushort UnkCount10;
            public ushort DriverChainCount;
            public ushort UnkCount12;
            public ushort UnkCount13;
            public ushort UnkCount14;
            public long Offset28;
            public long MapOffset;
            public teResourceGUID UnknownGuidx81;
            public long unknown2;
            public teResourceGUID Unknown3;
            public long Unknown4;
            public long Unknown5;
            public long Unknown6;

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
            public short VerticalParent;
            public ClothNodeWeight[] Bones;
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

                    if (Header.Count > 0) {
                        for (ulong i = 0; i < Header.Count; i++) {
                            Descriptors[i] = reader.Read<ClothDesc>();

                            long nextStartPos = reader.BaseStream.Position;

                            reader.BaseStream.Position = Descriptors[i].BoneSectionOffset;
                            NodeBones[i] = new Dictionary<int, short>();
                            for (int nodeIndex = 0; nodeIndex < Descriptors[i].DriverNodeCount; nodeIndex++) {
                                NodeBones[i][nodeIndex] = reader.ReadInt16();
                            }
                            
                            reader.BaseStream.Position = Descriptors[i].VertParent1Offset;
                            var vertParent1 = reader.ReadArray<short>(Descriptors[i].DriverNodeCount);
                            reader.BaseStream.Position = Descriptors[i].WeightOffset;
                            var weight = reader.ReadArray<float>(Descriptors[i].DriverNodeCount * 4);
                            reader.BaseStream.Position = Descriptors[i].IndicesOffset;
                            var indices = reader.ReadArray<short>(Descriptors[i].DriverNodeCount * 4);
                            
                            Nodes[i] = new ClothNode[Descriptors[i].DriverNodeCount];
                            for (int nodeIndex = 0; nodeIndex < Descriptors[i].DriverNodeCount; nodeIndex++) {
                                Nodes[i][nodeIndex] = new ClothNode {
                                    VerticalParent = vertParent1[nodeIndex],
                                    Bones = new[] {
                                        new ClothNodeWeight(indices[nodeIndex * 4], weight[nodeIndex * 4]),
                                        new ClothNodeWeight(indices[nodeIndex * 4 + 1], weight[nodeIndex * 4 + 1]),
                                        new ClothNodeWeight(indices[nodeIndex * 4 + 2], weight[nodeIndex * 4 + 2]),
                                        new ClothNodeWeight(indices[nodeIndex * 4 + 3], weight[nodeIndex * 4 + 3]),
                                    }
                                };
                            }

                            reader.BaseStream.Position = nextStartPos;
                        }
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
                            if (NodeBones[clothIndex][parentRaw] == -1) { // todo: this is unpredictable with new cloth
                                ClothNodeWeight weightedBone =
                                    node.Bones.Aggregate((i1, i2) => i1.Weight > i2.Weight ? i1 : i2);
                                hierarchy[NodeBones[clothIndex][nodeIndex]] = weightedBone.Bone;
                            }
                        }
                    } else {
                        if (NodeBones[clothIndex].ContainsKey(nodeIndex)) { // todo: this is unpredictable with new cloth
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
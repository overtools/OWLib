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
            public long Section1Offset;
            public long Section2Offset;
            public long Section3Offset;
            public long Section4Offset;
            public long Section5Offset;
            public long Section6Offset;
            public long Section7Offset;
            public long Section8Offset;
            public long Section9Offset;
            public long Section10Offset;
            public long Section11Offset;
            public long Section12Offset;
            public long Section13Offset;
            public long Section14Offset;
            public long Section15Offset;
            public long Section16Offset;
            public long Section17Offset;
            public long Section18Offset;
            public long Section19Offset;
            public long Section20Offset;
            public long Section21Offset;
            public long Section22Offset;
            public long Section23Offset;
            public long Section24Offset;
            public long Section25Offset;
            public long Section26Offset;
            public fixed byte Name[32];
            public ushort Count1;
            public ushort Count2;
            public ushort Count3;
            public ushort Count4;
            public ushort Count5;
            public ushort Count6;
            public ushort Count7;
            public ushort Count8;
            public ushort Count9;
            public ushort Count10;
            public ushort Count11;
            public ushort Count12;
            public ushort Count13;
            public ushort Count14;
            public ushort Count15;
            public ushort Count16;
            public ushort Count17;
            public ushort Count18;
            public ushort Count19;
            public ushort Count20;
            public long BoneIndicesTableOffset;
            public teResourceGUID UnkGUID1;
            public teResourceGUID UnkGUID2;
            public teResourceGUID UnkGUID3;
            public teResourceGUID UnkGUID4;
            public ulong Unk1;
            public int ClothBonesUsed;
            public int Unk2;

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

        public ushort[][] BoneLookup;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<ClothHeader>();

                if (Header.Offset > 0) {
                    reader.BaseStream.Position = Header.Offset;

                    Descriptors = new ClothDesc[Header.Count];
                    // Nodes = new ClothNode[Header.Count][];
                    // NodeBones = new Dictionary<int, short>[Header.Count];
                    BoneLookup = new ushort[Header.Count][];

                    if (Header.Count > 0) {
                        for (ulong i = 0; i < Header.Count; i++) {
                            Descriptors[i] = reader.Read<ClothDesc>();

                            long nextStartPos = reader.BaseStream.Position;

                            reader.BaseStream.Position = Descriptors[i].BoneIndicesTableOffset;
                            BoneLookup[i] = reader.ReadArray<ushort>(Descriptors[i].ClothBonesUsed);

                            // if (Descriptors[i].Section8Offset != 4574069944263674675) { // todo: wtf
                            //     reader.BaseStream.Position = Descriptors[i].Section8Offset;
                            //     NodeBones[i] = new Dictionary<int, short>();
                            //     for (int nodeIndex = 0; nodeIndex < Descriptors[i].ClothBonesUsed; nodeIndex++) {
                            //         NodeBones[i][nodeIndex] = reader.ReadInt16();
                            //     }
                            // }
                            //
                            // if (Descriptors[i].Section1Offset > 0 && Descriptors[i].Section1Offset != 4692750811720056850) { // todo: wtf2
                            //     Nodes[i] = new ClothNode[Descriptors[i].ClothBonesUsed];
                            //     reader.BaseStream.Position = Descriptors[i].Section1Offset;
                            //     for (int nodeIndex = 0; nodeIndex < Descriptors[i].ClothBonesUsed; nodeIndex++) {
                            //         Nodes[i][nodeIndex] = new ClothNode(reader);
                            //     }
                            // }

                            reader.BaseStream.Position = nextStartPos;
                        }
                    }
                }
            }
        }

        public short[] CreateClothHierarchy(teModelChunk_Skeleton skeleton, out Dictionary<int, ClothNode> nodeMap) {
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

        public short[] CreateFakeHierarchy(teModelChunk_Skeleton skeleton, teModelChunk_RenderMesh.Submesh[] submeshes, out HashSet<int> nodeMap) {
            short[] hierarchy = (short[]) skeleton.Hierarchy.Clone();
            nodeMap = new HashSet<int>();

            var guideBones = new Dictionary<int, teVec3>();
            var seenBones = new HashSet<int>();
            foreach (var submesh in submeshes) {
                for (var index = 0; index < submesh.BoneIndices.Length; index++) {
                    for (var i = 0; i < submesh.BoneIndices[index].Length; i++) {
                        var bone = submesh.BoneIndices[index][i];
                        var weight = submesh.BoneWeights[index][i];
                        if (weight < 0.001f) {
                            continue;
                        }

                        if (bone >= skeleton.Lookup.Length) {
                            continue;
                        }

                        seenBones.Add(skeleton.Lookup[bone]);
                    }
                }
            }

            for (var index = 0; index < skeleton.Hierarchy.Length; index++) {
                if (seenBones.Contains(index)) {
                    continue;
                }

                skeleton.GetWorldSpace(index, out _, out _, out var pos);
                guideBones[index] = pos;
            }

            foreach (var cloth in BoneLookup) {
                foreach (var bone in cloth) {
                    if (nodeMap.Add(bone)) {
                        skeleton.GetWorldSpace(bone, out _, out _, out var src);

                        var best = float.MaxValue;
                        foreach (var (testBone, testPos) in guideBones) {
                            const float bias = 2 / 3f;
                            var upwardsBias = testPos.Y > src.Y ? bias : 1f;
                            if ((testPos - src).Length() * upwardsBias < best) {
                                hierarchy[bone] = (short) testBone;
                                best = (testPos - src).Length() * upwardsBias;
                            }
                        }
                    }
                }
            }

            return hierarchy;
        }
    }
}
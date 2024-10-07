using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace TankLib.Chunks {
    public class teModelChunk_Cloth : IChunk {
        public string ID => "CLTH";
        public List<IChunk> SubChunks { get; set; }

        /// <summary>CLTH header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ClothHeader {
            public ulong Count;
            public long Offset;
        }

        // this is a shared format
        // see dmClothDataMirror in d4
        // (not quite identical)
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct DominoClothData {
            public long PositionsOffset;
            public long SectionOffset3;
            public long SectionOffset4;
            public long SectionOffset5;
            public long SectionOffset6;
            public long SectionOffset7;
            public long SectionOffset8;
            public long m_vertexParentIndices;
            public long SectionOffset10;
            public long SectionOffset11;
            public long m_vertexDriverWeights;
            public long m_vertexDriverInfluences;
            public long m_vertexFollowerBoneIndices;
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
            public long m_driverToBoneMap;
            public fixed byte Name[32];
            public int Unknown1;
            public ushort m_vertexCount;
            public ushort UnkCount2;
            public ushort UnkCount3;
            public ushort UnkCount4;
            public ushort UnkCount5;
            public ushort UnkCount6;
            public ushort UnkCount7;
            public ushort UnkCount8;
            public ushort UnkCount9;
            public ushort UnkCount10;
            public ushort UnkCount11;
            public ushort m_boneCount;
            public ushort UnkCount13;
            public ushort UnkCount14;
        }

        /// <summary>Cloth description</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ClothDesc {
            public DominoClothData m_dominoData;
            
            public long Offset28;
            public long MapOffset;
            public teResourceGUID UnknownGuidx81;
            public long unknown2;
            public teResourceGUID Unknown3;
            public long Unknown4;
            public long Unknown5;
            public long Unknown6;
        }

        private struct VertexDriverWeight {
            public short DriverIdx;
            public float Weight;

            public VertexDriverWeight(short driverIdx, float weight) {
                DriverIdx = driverIdx;
                Weight = weight;
            }
        }

        /// <summary>Cloth node</summary>
        private struct ClothVertex {
            public short FollowerBone;
            public short VerticalParent;
            public VertexDriverWeight[] DriverWeights;
        }
        
        private ClothHeader Header;
        private ClothPiece[] Pieces;

        private class ClothPiece {
            public ClothDesc Descriptor;
            public ClothVertex[] Vertices;
            public short[] BoneToDriverMap;
        }

        public void Parse(Stream input) {
            using BinaryReader reader = new BinaryReader(input);
            Header = reader.Read<ClothHeader>();
            reader.BaseStream.Position = Header.Offset;

            Pieces = new ClothPiece[Header.Count];
            
            for (ulong i = 0; i < Header.Count; i++) {
                var descriptor = reader.Read<ClothDesc>();
                long nextDescriptorPos = reader.BaseStream.Position;

                reader.BaseStream.Position = descriptor.m_dominoData.m_vertexFollowerBoneIndices;
                var followerBoneIndices = reader.ReadArray<short>(descriptor.m_dominoData.m_vertexCount);
                    
                reader.BaseStream.Position = descriptor.m_dominoData.m_vertexParentIndices;
                var vertexParentIndices = reader.ReadArray<short>(descriptor.m_dominoData.m_vertexCount);
                
                reader.BaseStream.Position = descriptor.m_dominoData.m_vertexDriverWeights;
                var vtxDriverWeights = reader.ReadArray<float>(descriptor.m_dominoData.m_vertexCount * 4);
                
                reader.BaseStream.Position = descriptor.m_dominoData.m_vertexDriverInfluences;
                var vtxDriverIndices = reader.ReadArray<short>(descriptor.m_dominoData.m_vertexCount * 4);
                
                reader.BaseStream.Position = descriptor.m_dominoData.m_driverToBoneMap;
                var boneToDriverMap = reader.ReadArray<short>(descriptor.m_dominoData.m_boneCount);
                    
                var nodes = new ClothVertex[descriptor.m_dominoData.m_vertexCount];
                for (int vtxIdx = 0; vtxIdx < descriptor.m_dominoData.m_vertexCount; vtxIdx++) {
                    nodes[vtxIdx] = new ClothVertex {
                        FollowerBone = followerBoneIndices[vtxIdx],
                        VerticalParent = vertexParentIndices[vtxIdx],
                        DriverWeights = [
                            new VertexDriverWeight(vtxDriverIndices[vtxIdx*4 + 0], vtxDriverWeights[vtxIdx*4 + 0]),
                            new VertexDriverWeight(vtxDriverIndices[vtxIdx*4 + 1], vtxDriverWeights[vtxIdx*4 + 1]),
                            new VertexDriverWeight(vtxDriverIndices[vtxIdx*4 + 2], vtxDriverWeights[vtxIdx*4 + 2]),
                            new VertexDriverWeight(vtxDriverIndices[vtxIdx*4 + 3], vtxDriverWeights[vtxIdx*4 + 3])
                        ]
                    };
                }

                Pieces[i] = new ClothPiece {
                    Descriptor = descriptor,
                    Vertices = nodes,
                    BoneToDriverMap = boneToDriverMap
                };
                reader.BaseStream.Position = nextDescriptorPos;
            }
        }

        private static short GetEffectiveBoneIndex(in HierarchyBuildContext ctx, short vertexIndex) {
            ref readonly var vertex = ref ctx.Piece.Vertices[vertexIndex];
            var parentVertex = vertex.VerticalParent;

            short parentBoneIndex;
            if (parentVertex == vertexIndex || parentVertex == -1) {
                // sometimes a vertex's parent is itself
                // presumably that means use driver
                // (definitely do not parent to self, blender will delete the bones -_-)
                
                var highestWeightedDriver = vertex.DriverWeights.MaxBy(x => x.Weight);
                if (highestWeightedDriver.DriverIdx == -1) return -1;

                parentBoneIndex = (short)ctx.Piece.BoneToDriverMap.AsSpan().IndexOf(highestWeightedDriver.DriverIdx);
            } else {
                parentBoneIndex = GetEffectiveBoneIndex(ctx, parentVertex);
            }

            if (vertex.FollowerBone == -1) {
                return parentBoneIndex;
            }
            
            if (parentBoneIndex != -1) {
                ctx.Hierarchy[vertex.FollowerBone] = parentBoneIndex;
                ctx.ReparentedBones.Add(vertex.FollowerBone);
            }
            return vertex.FollowerBone;
        }

        private struct HierarchyBuildContext {
            public short[] Hierarchy;
            public HashSet<short> ReparentedBones;
            public ClothPiece Piece;
        }

        public short[] CreateFakeHierarchy(teModelChunk_Skeleton skeleton, out HashSet<short> reparentedBones) {
            short[] hierarchy = (short[]) skeleton.Hierarchy.Clone();
            reparentedBones = new HashSet<short>();
            
            var ctx = new HierarchyBuildContext {
                Hierarchy = hierarchy,
                ReparentedBones = reparentedBones,
                Piece = null!
            };

            foreach (var piece in Pieces) {
                ctx.Piece = piece;

                for (int i = 0; i < piece.Vertices.Length; i++) {
                    // some duplicate work, but doesn't really matter
                    GetEffectiveBoneIndex(ctx, (short)i);
                }
            }

            return hierarchy;
        }
    }
}
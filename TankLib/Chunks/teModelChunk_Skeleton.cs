using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using SharpDX;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>mskl: Defines model skeleton</summary>
    public class teModelChunk_Skeleton : IChunk {
        public string ID => "mskl";
        public List<IChunk> SubChunks { get; set; }

        /// <summary>mskl header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SkeletonHeader {
            public long Hierarchy1Offset; // 0
            public long Matrix44Offset; // 8
            public long Matrix44iOffset; // 16
            public long Matrix43Offset; // 24
            public long Matrix43iOffset; // 32
            public long Struct6Offset;  // ? 40
            public long m_new48; // n/a -> 48
            public long IDOffset; // 48 -> 56
            public long NameOffset;  // ? 56 -> 64
            public long Struct9Offset; // 64 -> 72
            public long RemapOffset; // 72 -> 80
            public long Hierarchy2Offset; // 80 -> 88
            public long m_new96; // n/a -> 96
            public int Unknown1; // 88 -> 104

            public ushort BonesAbs; // 92 -> 108
            public ushort BonesSimple;
            public ushort BonesCloth;
            public ushort RemapCount;
            public ushort IDCount;

            // ... idk. something added tho
        }

        /// <summary>Header data</summary>
        public SkeletonHeader Header;

        public teMtx44[] Matrices;
        public teMtx44[] MatricesInverted;
        public teMtx43[] Matrices34;
        public teMtx43[] Matrices34Inverted;
        public short[] Hierarchy;
        public ushort[] Lookup;
        // ReSharper disable once InconsistentNaming
        public uint[] IDs;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<SkeletonHeader>();

                Hierarchy = new short[Header.BonesAbs];

                if (Header.Hierarchy1Offset > 0) {
                    input.Position = Header.Hierarchy1Offset;
                    for(int i = 0; i < Header.BonesAbs; ++i) {
                        input.Position += 4L;
                        Hierarchy[i] = reader.ReadInt16();
                    }
                }

                Matrices = new teMtx44[Header.BonesAbs];
                MatricesInverted = new teMtx44[Header.BonesAbs];
                Matrices34 = new teMtx43[Header.BonesAbs];
                Matrices34Inverted = new teMtx43[Header.BonesAbs];

                if (Header.Matrix44Offset > 0) {
                    input.Position = Header.Matrix44Offset;
                    Matrices = reader.ReadArray<teMtx44>(Header.BonesAbs);
                }

                if (Header.Matrix44iOffset > 0) {
                    input.Position = Header.Matrix44iOffset;
                    MatricesInverted = reader.ReadArray<teMtx44>(Header.BonesAbs);
                }

                if (Header.Matrix43Offset > 0) {
                    input.Position = Header.Matrix43Offset;
                    Matrices34 = reader.ReadArray<teMtx43>(Header.BonesAbs);
                }

                if (Header.Matrix43iOffset > 0) {
                    input.Position = Header.Matrix43iOffset;
                    Matrices34Inverted = reader.ReadArray<teMtx43>(Header.BonesAbs);
                }

                Lookup = new ushort[Header.RemapCount];
                if (Header.RemapOffset > 0) {
                    input.Position = Header.RemapOffset;
                    Lookup = reader.ReadArray<ushort>(Header.RemapCount);
                }

                IDs = new uint[Header.IDCount];
                // todo: CLOTH BONES WHERE GONE.
                if (Header.IDOffset > 0) {
                    input.Position = Header.IDOffset;
                    IDs = reader.ReadArray<uint>(Header.IDCount);
                }
            }
        }

        public void GetWorldSpace(int idx, out teVec3 scale, out teQuat rotation, out teVec3 translation) {
            teMtx43 parBoneMat = idx == -1 ? teMtx43.Identity() : Matrices34[idx];
            scale = new teVec3(parBoneMat[1, 0], parBoneMat[1, 1], parBoneMat[1, 2]);
            rotation = new teQuat(parBoneMat[0, 0], parBoneMat[0, 1],parBoneMat[0, 2], parBoneMat[0, 3]);
            translation = new teVec3(parBoneMat[2, 0], parBoneMat[2, 1], parBoneMat[2, 2]);
        }

        public Matrix GetWorldSpace(int idx) {
            GetWorldSpace(idx, out teVec3 scale, out teQuat rotation, out teVec3 translation);
            return Matrix.Scaling(scale) *
                   Matrix.RotationQuaternion(rotation) *
                   Matrix.Translation(translation);
        }
    }
}
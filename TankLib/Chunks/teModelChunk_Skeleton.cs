using System.IO;
using System.Runtime.InteropServices;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>mskl: Defines model skeleton</summary>
    public class teModelChunk_Skeleton : IChunk {
        public string ID => "mskl";
        
        /// <summary>mskl header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SkeletonHeader {
            public long Hierarchy1Offset;
            public long Matrix44Offset;
            public long Matrix44iOffset;
            public long Matrix43Offset;
            public long Matrix43iOffset;
            public long Struct6Offset;  // ?
            public long IDOffset;
            public long NameOffset;  // ?
            public long Struct9Offset;
            public long RemapOffset;
            public long Hierarchy2Offset;
            public int Unknown1;
            public ushort BonesAbs;
            public ushort BonesSimple;
            public ushort BonesCloth;
            public ushort RemapCount;
            public ushort IDCount;
            public ushort UnknownStruct6Count;
            public ushort Unknown4;
            public ushort NameCount;
            public ushort UnknownStruct9Count;
            public ushort Unknown5;
            public ushort PaddingSize;
        }
        
        /// <summary>Header data</summary>
        public SkeletonHeader Header;
        
        public teMtx44A[] Matrices;
        public teMtx44A[] MatricesInverted;
        public teMtx43A[] Matrices34;
        public teMtx43A[] Matrices34Inverted;
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
                
                // todo: should be 3x4 mat not 4x3?

                Matrices = new teMtx44A[Header.BonesAbs];
                MatricesInverted = new teMtx44A[Header.BonesAbs];
                Matrices34 = new teMtx43A[Header.BonesAbs];
                Matrices34Inverted = new teMtx43A[Header.BonesAbs];

                if (Header.Matrix44Offset > 0) {
                    input.Position = Header.Matrix44Offset;
                    Matrices = reader.ReadArray<teMtx44A>(Header.BonesAbs);
                }

                if (Header.Matrix44iOffset > 0) {
                    input.Position = Header.Matrix44iOffset;
                    MatricesInverted = reader.ReadArray<teMtx44A>(Header.BonesAbs);
                }

                if (Header.Matrix43Offset > 0) {
                    input.Position = Header.Matrix43Offset;
                    Matrices34 = reader.ReadArray<teMtx43A>(Header.BonesAbs);
                }

                if (Header.Matrix43iOffset > 0) {
                    input.Position = Header.Matrix43iOffset;
                    Matrices34Inverted = reader.ReadArray<teMtx43A>(Header.BonesAbs);
                }

                Lookup = new ushort[Header.RemapCount];
                if (Header.RemapOffset > 0) {
                    input.Position = Header.RemapOffset;
                    Lookup = reader.ReadArray<ushort>();
                }

                IDs = new uint[Header.BonesAbs];
                if (Header.IDOffset > 0) {
                    input.Position = Header.IDOffset;
                    IDs = reader.ReadArray<uint>();
                }
            }
        }
    }
}
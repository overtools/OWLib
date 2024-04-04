using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TankLib.Math;

namespace TankLib {
    /// <summary>Tank Animation, type 006</summary>
    public class teAnimation {
        [Flags]
        public enum AnimFlags : ushort {
            // todo
        }

        [Flags]
        public enum InfoTableFlags : ushort {
            F1 = 0x1,
            F2 = 0x2,
            F4 = 0x4,
            F8 = 0x8,
            F16 = 0x10,
            F32 = 0x20,
            F64 = 0x40,
            F128 = 0x80,
            NewScaleCompression = 0x100, // todo: implement me
            LegacyPositionFormat = 0x200,
            NewRotationCompression = 0x400,
            F2048 = 0x800
        }

        /// <summary>Animation header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnimHeader {
            /// <summary>
            /// Animation "priority". Is 16 for many animations.
            /// </summary>
            public byte Priority;

            /// <summary>
            /// Animation "group"? layer flags?
            /// </summary>
            public byte Group;

            /// <summary>Number of bones animated</summary>
            public ushort BoneCount;

            /// <summary>Unknown flags</summary>
            public AnimFlags Flags;

            public ushort Unknown1;

            public uint Unknown2;

            public ushort Unknown3;

            public ushort Unknown4;

            public uint Unknown5;

            /// <summary>Duration, in seconds</summary>
            public float Duration;

            public float Unknown6;

            /// <summary>Frames Per Second</summary>
            public float FPS;

            public float Unknown7;

            public float Unknown8;

            /// <summary>AnimationEffect reference</summary>
            /// <remarks>File type 08F</remarks>
            public teResourceGUID Effect;

            public long EffectStorage;

            /// <summary>Offset to bone list</summary>
            public long BoneListOffset;

            public long Unknown9;

            public long Unknown10;

            public long Unknown11;

            public long Unknown12;

            public long Unknown13;

            public long SomeTableOffset; // new animation param table, usually float tracks

            /// <summary>Offset to info table</summary>
            public long InfoTableOffset;

            public long Padding;
        }

        public struct InfoTable {
            /// <summary>Number of scales</summary>
            public ushort ScaleCount;

            /// <summary>Number of positions</summary>
            public ushort PositionCount;

            /// <summary>Number of rotations</summary>
            public ushort RotationCount;

            /// <summary>Unknown flags</summary>
            public InfoTableFlags Flags;

            /// <summary>Offset to scale indices</summary>
            public int ScaleIndicesOffset;

            /// <summary>Offset to position indices</summary>
            public int PositionIndicesOffset;

            /// <summary>Offset to rotation indices</summary>
            public int RotationIndicesOffset;

            /// <summary>Offset to scale data</summary>
            public int ScaleDataOffset;

            /// <summary>Offset to postion data</summary>
            public int PositionDataOffset;

            /// <summary>Offset to rotation data</summary>
            public int RotationDataOffset;
        }

        public class BoneAnimation {
            public Dictionary<int, teVec3> Positions;
            public Dictionary<int, teVec3> Scales;
            public Dictionary<int, teQuat> Rotations;

            public BoneAnimation() {
                Positions = new Dictionary<int, teVec3>();
                Scales = new Dictionary<int, teVec3>();
                Rotations = new Dictionary<int, teQuat>();
            }
            // key = frame number; value = abs value this frame
            // then slerp between or whatever
        }

        /// <summary>Header data</summary>
        public AnimHeader Header;

        /// <summary>Number of info tables</summary>
        public int InfoTableSize;

        /// <summary>Bone IDs</summary>
        public int[] BoneList;

        /// <summary>Bone info tables</summary>
        public InfoTable[] InfoTables;

        public BoneAnimation[] BoneAnimations;

        /// <summary>Load animation from a stream</summary>
        public teAnimation(Stream stream, bool keepOpen = false) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, keepOpen)) {
                Header = reader.Read<AnimHeader>();

                InfoTableSize = (int)(Header.FPS * Header.Duration) + 1;

                reader.BaseStream.Position = Header.BoneListOffset;
                BoneList = reader.ReadArray<int>(Header.BoneCount);
                
                BoneAnimations = new BoneAnimation[Header.BoneCount];

                // todo: read data for non-bone animations

                reader.BaseStream.Position = Header.InfoTableOffset;
                var nextTableOffset = reader.BaseStream.Position;
                InfoTables = reader.ReadArray<InfoTable>(Header.BoneCount);

                for (int boneIndex = 0; boneIndex < Header.BoneCount; boneIndex++) {
                    var streamPos = nextTableOffset;
                    nextTableOffset += Unsafe.SizeOf<InfoTable>();
                    var infoTable = InfoTables[boneIndex];

                    long scaleIndicesPos = (long)infoTable.ScaleIndicesOffset * 4 + streamPos;
                    long positionIndicesPos = (long)infoTable.PositionIndicesOffset * 4 + streamPos;
                    long rotationIndicesPos = (long)infoTable.RotationIndicesOffset * 4 + streamPos;
                    long scaleDataPos = (long)infoTable.ScaleDataOffset * 4 + streamPos;
                    long positionDataPos = (long)infoTable.PositionDataOffset * 4 + streamPos;
                    long rotationDataPos = (long)infoTable.RotationDataOffset * 4 + streamPos;

                    reader.BaseStream.Position = scaleIndicesPos;
                    int[] scaleIndices = ReadIndices(reader, infoTable.ScaleCount);

                    reader.BaseStream.Position = positionIndicesPos;
                    int[] positionIndices = ReadIndices(reader, infoTable.PositionCount);

                    reader.BaseStream.Position = rotationIndicesPos;
                    int[] rotationIndices = ReadIndices(reader, infoTable.RotationCount);

                    BoneAnimation boneAnimation = new BoneAnimation();
                    BoneAnimations[boneIndex] = boneAnimation;

                    reader.BaseStream.Position = scaleDataPos;
                    for (int j = 0; j < infoTable.ScaleCount; j++) {
                        int frame = System.Math.Abs(scaleIndices[j]) % InfoTableSize;
                        boneAnimation.Scales[frame] = ReadScale(reader);
                    }

                    reader.BaseStream.Position = positionDataPos;
                    bool useNewPositionFormat = (infoTable.Flags & InfoTableFlags.LegacyPositionFormat) == 0; // If this flag isn't set, use the new 10 bytes format, otherwise use the 12 bytes one
                    for (int j = 0; j < infoTable.PositionCount; j++) {
                        int frame = System.Math.Abs(positionIndices[j]) % InfoTableSize;
                        boneAnimation.Positions[frame] = ReadPosition(reader, useNewPositionFormat);
                    }
                    
                    reader.BaseStream.Position = rotationDataPos;
                    bool useNewRotationFormat = infoTable.Flags.HasFlag(InfoTableFlags.NewRotationCompression);
                    for (int j = 0; j < infoTable.RotationCount; j++) {
                        int frame = System.Math.Abs(rotationIndices[j]) % InfoTableSize;
                        boneAnimation.Rotations[frame] = ReadRotation(reader, useNewRotationFormat);
                    }
                    
                    //if (infoTable.PositionCount > 0) {
                    //    Console.Out.WriteLine($"bytes per position: {(rotationDataPos-positionDataPos) / (float)infoTable.PositionCount}: {useNewPositionFormat}");
                    //}
                    
                    //if (infoTable.PositionCount > 0 && boneIndex < Header.BoneCount-1 && infoTable.RotationCount > 1) {
                    //    var nextTable = infoTables[boneIndex + 1];
                    //    var nextScalesOffset = (long)nextTable.ScaleIndicesOffset * 4 + nextTableOffset;
                    //    Console.Out.WriteLine($"bytes per rotation: {(nextScalesOffset-rotationDataPos) / (float)infoTable.RotationCount}: {useNewRotationFormat}");
                    //}
                }
            }
        }
        
        private static teQuat ReadRotation(BinaryReader reader, bool newFormat) {
            if (newFormat) {
                ushort a = reader.ReadUInt16();
                ushort b = reader.ReadUInt16();
                ushort c = reader.ReadUInt16();
                ushort d = reader.ReadUInt16();
                return UnpackNewRotation(a, b, c, d);
            } else {
                ushort a = reader.ReadUInt16();
                ushort b = reader.ReadUInt16();
                ushort c = reader.ReadUInt16();
                return UnpackOldRotation(a, b, c);
            }
        }
        
        private static teQuat UnpackNewRotation(ushort a, ushort b, ushort c, ushort d) {
            var aPre1 = a << 5 | (d & 0x1F);
            var bPre1 = b << 4 | ((d >> 5) & 0xF);
            var cPre1 = c << 5 | ((d >> 9) & 0x1F);
            
            var aDecoded = 0.5f * 1.41421f * (aPre1 - 0x100000) / 0x100000;
            var bDecoded = 0.5f * 1.41421f * (bPre1 - 0x80000) / 0x80000;
            var cDecoded = 0.5f * 1.41421f * (cPre1 - 0x100000) / 0x100000;
            var dDecoded = CalcRotationW(aDecoded, bDecoded, cDecoded);
            
            var axis = d >> 14;
            return QuatFromAxisOrder(axis, aDecoded, bDecoded, cDecoded, dDecoded);
        }
        
        private static teQuat UnpackOldRotation(ushort a, ushort b, ushort c) {
            int axis1 = a >> 15;
            int axis2 = b >> 15;
            int axis = axis2 << 1 | axis1;

            a = (ushort)(a & 0x7FFF);
            b = (ushort)(b & 0x7FFF);

            var x = 1.41421f * (a - 0x4000) / 0x8000;
            var y = 1.41421f * (b - 0x4000) / 0x8000;
            var z = 1.41421f * (c - 0x8000) / 0x10000;
            var w = CalcRotationW(x, y, z);

            // Console.Out.WriteLine("Unpack Values: X: {0}, Y: {1}, Z: {2}, W: {3}, Axis: {4}", x, y, z, w, axis);

            return QuatFromAxisOrder(axis, x, y, z, w);
        }

        private static float CalcRotationW(float x, float y, float z) {
            return MathF.Pow(1.0f - MathF.Min(x * x + y * y + z * z, 1), 0.5f);
        }

        private static teQuat QuatFromAxisOrder(int axis, float x, float y, float z, float w) {
            if (axis == 0) return new teQuat(w, x, y, z);
            if (axis == 1) return new teQuat(x, w, y, z);
            if (axis == 2) return new teQuat(x, y, w, z);
            if (axis == 3) return new teQuat(x, y, z, w);
            throw new Exception($"Unknown Axis detected! Axis: {axis}");
        }

        /// <summary>
        /// Read position value
        /// </summary>
        /// <param name="reader">Source reader</param>
        /// <returns>Position value</returns>
        private static teVec3 ReadPosition(BinaryReader reader, bool newFormat) {
            if (newFormat) {
                float x = (float) reader.ReadHalf();
                float y = (float) reader.ReadHalf();
                float z = (float) reader.ReadHalf();

                uint packedData = reader.ReadUInt32();
                // var unkX = packedData & 0x3FF;
                // var unkY = (packedData >> 10) & 0x3FF; // <-- sometimes this value changes with no changes to any component. a single bit gets flipped.
                // var unkZ = (packedData >> 20) & 0x3FF; // <-- changes wildly, indicating that my bit shifting/masking is wrong
                // var unk = packedData >> 30; // ????

                return new teVec3(x / 32f, y / 32f, z / 32f);
            } else {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();

                return new teVec3(x, y, z);
            }
        }

        /// <summary>
        /// Read scale value
        /// </summary>
        /// <param name="reader">Source reader</param>
        /// <returns>Scale value</returns>
        private static teVec3 ReadScale(BinaryReader reader) {
            ushort x = reader.ReadUInt16();
            ushort y = reader.ReadUInt16();
            ushort z = reader.ReadUInt16();

            return UnpackScale(x, y, z);
        }

        /// <summary>
        /// Unpack scale value
        /// </summary>
        /// <param name="a">Packed X component</param>
        /// <param name="b">Packed Y component</param>
        /// <param name="c">Packed Z component</param>
        /// <returns>Unpacked scale value</returns>
        private static teVec3 UnpackScale(ushort a, ushort b, ushort c) {
            double x = a / 1024d;
            double y = b / 1024d;
            double z = c / 1024d;

            return new teVec3(x, y, z);
        }

        /// <summary>
        /// Read frame indices
        /// </summary>
        /// <param name="reader">Source BinaryReader</param>
        /// <param name="count">Number of indices</param>
        /// <returns>Frame indices</returns>
        private int[] ReadIndices(BinaryReader reader, ushort count) {
            int[] ret = new int[count];
            for (int i = 0; i < count; i++) {
                if (InfoTableSize <= 255) {
                    ret[i] = reader.ReadByte();
                } else {
                    ret[i] = reader.ReadInt16();
                }
            }
            return ret;
        }
    }
}
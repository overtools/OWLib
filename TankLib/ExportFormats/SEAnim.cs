using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using TankLib.Math;

namespace TankLib.ExportFormats {
    // ReSharper disable once InconsistentNaming
    public class SEAnim : IExportFormat {
        public string Extension => "seanim";
        
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("SEAnim");
        private const ushort Version = 0x1;
        private const ushort HeaderSize = 0x1C;

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SEAnimType : byte {
            Absolute = 0,
            Additive = 1,
            Relative = 2,
            Delta = 3
        }

        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SEAnimPresence : byte {
            BoneLocation = 1,
            BoneRotation = 2,
            BoneScale = 4,
            Reserved1 = 8,
            Reserved2 = 16,
            Reserved3 = 32,
            Note = 64,
            Custom = 128
        }

        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SEAnimProperty : byte {
            HighPrecision = 1,
            Reserved1 = 2,
            Reserved2 = 4,
            Reserved3 = 8,
            Reserved4 = 16,
            Reserved5 = 32,
            Reserved6 = 64,
            Reserved7 = 128
        }

        [Flags]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        private enum SEAnimFlags : byte {
            Looped = 1,
            Reserved1 = 2,
            Reserved2 = 4,
            Reserved3 = 8,
            Reserved4 = 16,
            Reserved5 = 32,
            Reserved6 = 64,
            Reserved7 = 128
        }

        public teAnimation Animation;
        private bool ScaleAnims;

        public SEAnim(teAnimation animation, bool scaleAnims) {
            Animation = animation;
            ScaleAnims = scaleAnims;
        }
        
        public void Write(Stream stream) {
            SEAnimPresence everHas = 0;

            foreach (teAnimation.BoneAnimation boneAnimation in Animation.BoneAnimations) {
                if (boneAnimation.Positions.Count > 0) {
                    everHas |= SEAnimPresence.BoneLocation;
                }
                if (boneAnimation.Scales.Count > 0) {
                    everHas |= SEAnimPresence.BoneScale;
                }
                if (boneAnimation.Rotations.Count > 0) {
                    everHas |= SEAnimPresence.BoneRotation;
                }
            }

            int frameCount = Animation.InfoTableSize - 1;
            
            byte frameWidth;
            if (frameCount <= 0xFF) {
                frameWidth = 1;
            } else if (frameCount <= 0xFFFF) {
                frameWidth = 2;
            } else {
                frameWidth = 4;
            }

            using (BinaryWriter writer = new BinaryWriter(stream)) {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write(HeaderSize);
                
                writer.Write((byte)SEAnimType.Absolute);
                writer.Write((byte)0);
                writer.Write((byte)everHas);
                writer.Write((byte)SEAnimProperty.HighPrecision);
                writer.Write(new byte[] { 0, 0 });
                writer.Write(Animation.Header.FPS);
                writer.Write(frameCount + 1);
                writer.Write(Animation.BoneList.Length);
                writer.Write((byte)0);
                writer.Write(new byte[] { 0, 0, 0 });
                writer.Write((uint)0);

                foreach (int boneID in Animation.BoneList) {
                    WriteString(writer, OverwatchModel.GetBoneName((uint)boneID));
                }

                foreach (teAnimation.BoneAnimation boneAnimation in Animation.BoneAnimations) {
                    writer.Write((byte)0);

                    if (everHas.HasFlag(SEAnimPresence.BoneLocation)) {
                        float scale = 1.0f;

                        if (ScaleAnims) {
                            scale = 2.54f;
                        }
                        WriteFrames3D(writer, frameWidth, boneAnimation.Positions, scale);
                    }

                    if (everHas.HasFlag(SEAnimPresence.BoneRotation)) {
                        WriteFrames4D(writer, frameWidth, boneAnimation.Rotations);
                    }

                    if (everHas.HasFlag(SEAnimPresence.BoneScale)) {
                        WriteFrames3D(writer, frameWidth, boneAnimation.Scales);
                    }
                }
            }
        }
        
        private static void WriteString(BinaryWriter writer, string str) {
            writer.Write(Encoding.Default.GetBytes(str));
            writer.Write((byte)0);
        }
        
        private static void WriteFrameT(BinaryWriter writer, byte frameWidth, int value) {
            if (frameWidth == 1) {
                writer.Write((byte)value);
            } else if (frameWidth == 2) {
                writer.Write((ushort)value);
            } else {
                writer.Write((uint)value);
            }
        }

        private static void WriteFrames3D(BinaryWriter writer, byte frameWidth, Dictionary<int, teVec3> frames, float fac = 1.0f) {
            WriteFrameT(writer, frameWidth, frames.Count);

            foreach (KeyValuePair<int, teVec3> pair in frames) {
                WriteFrameT(writer, frameWidth, pair.Key);
                writer.Write((double)pair.Value.X * fac);
                writer.Write((double)pair.Value.Y * fac);
                writer.Write((double)pair.Value.Z * fac);
            }
        }

        private static void WriteFrames4D(BinaryWriter writer, byte frameWidth, Dictionary<int, teQuat> frames) {
            WriteFrameT(writer, frameWidth, frames.Count);

            foreach (KeyValuePair<int, teQuat> pair in frames) {
                WriteFrameT(writer, frameWidth, pair.Key);
                writer.Write((double)pair.Value.X);
                writer.Write((double)pair.Value.Y);
                writer.Write((double)pair.Value.Z);
                writer.Write((double)pair.Value.W);
            }
        }
    }
}

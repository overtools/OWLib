using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types;
using OWLib.Types.Map;

namespace OWLib.Writer {
    public class SEAnimWriter : IDataWriter {
        public string Format => ".seanim";
        public char[] Identifier => new char[1] { 'S' };
        public string Name => "SEAnim";
        public WriterSupport SupportLevel => WriterSupport.BONE | WriterSupport.POSE | WriterSupport.ANIM;

        private static byte[] MAGIC = Encoding.ASCII.GetBytes("SEAnim");
        private static ushort VERSION = 0x1;
        private static ushort HEADER_SZ = 0x1C;

        private enum SEAnimType : byte {
            ABSOLUTE = 0,
            ADDITIVE = 1,
            RELATIVE = 2,
            DELTA = 3
        }

        [Flags]
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

        public void WriteCString(BinaryWriter writer, string str) {
            writer.Write(Encoding.Default.GetBytes(str));
            writer.Write((byte)0);
        }

        public void WriteFrameT(BinaryWriter writer, byte frame_t, int value) {
            if (frame_t == 1) {
                writer.Write((byte)value);
            } else if (frame_t == 2) {
                writer.Write((ushort)value);
            } else {
                writer.Write((uint)value);
            }
        }

        public void WriteFrames3d(BinaryWriter writer, byte frame_t, SortedList<int, object> frames) {
            WriteFrameT(writer, frame_t, frames.Count);

            foreach (KeyValuePair<int, object> pair in frames) {
                Vec3d value = (Vec3d)pair.Value;
                WriteFrameT(writer, frame_t, pair.Key);
                writer.Write(value.x);
                writer.Write(value.y);
                writer.Write(value.z);
            }
        }

        public void WriteFrames4d(BinaryWriter writer, byte frame_t, SortedList<int, object> frames) {
            WriteFrameT(writer, frame_t, frames.Count);

            foreach (KeyValuePair<int, object> pair in frames) {
                Vec4d value = (Vec4d)pair.Value;
                WriteFrameT(writer, frame_t, pair.Key);
                writer.Write(value.x);
                writer.Write(value.y);
                writer.Write(value.z);
                writer.Write(value.w);
            }
        }

        public bool Write(Animation anim, Stream output, object[] data) {
            Dictionary<int, Dictionary<AnimChannelID, SortedList<int, object>>> framesByBone = new Dictionary<int, Dictionary<AnimChannelID, SortedList<int, object>>>();
            SortedSet<int> boneIds = new SortedSet<int>();

            SEAnimPresence everHas = 0;
            int frameCount = 0;

            foreach (Keyframe keyframe in anim.Animations) {
                int pos = keyframe.FramePositionI;
                if (pos > frameCount) {
                    frameCount = pos;
                }

                foreach (BoneAnimation bone in keyframe.BoneFrames) {
                    foreach (FrameValue value in bone.Values) {
                        everHas |= (SEAnimPresence)value.Channel;

                        if (!framesByBone.ContainsKey(bone.BoneID)) {
                            boneIds.Add(bone.BoneID);
                            framesByBone[bone.BoneID] = new Dictionary<AnimChannelID, SortedList<int, object>>();
                        }

                        if (!framesByBone[bone.BoneID].ContainsKey(value.Channel)) {
                            framesByBone[bone.BoneID][value.Channel] = new SortedList<int, object>();
                        }

                        framesByBone[bone.BoneID][value.Channel].Add(pos, value.Value);
                    }
                }
            }

            if (everHas == 0) {
                return false;
            }

            byte frameWidth;
            if (frameCount <= 0xFF) {
                frameWidth = 1;
            } else if (frameCount <= 0xFFFF) {
                frameWidth = 2;
            } else {
                frameWidth = 4;
            }
            /*
            byte boneWidth;
            if(boneIds.Count <= 0xFF) {
              boneWidth = 1;
            } else if(boneIds.Count <= 0xFFFF) {
              boneWidth = 2;
            } else {
              boneWidth = 4;
            }
            */

            using (BinaryWriter writer = new BinaryWriter(output, Encoding.Default, false)) {
                writer.Write(MAGIC);
                writer.Write(VERSION);

                writer.Write(HEADER_SZ);
                writer.Write((byte)SEAnimType.ABSOLUTE);
                writer.Write((byte)0);
                writer.Write((byte)everHas);
                writer.Write((byte)SEAnimProperty.HighPrecision);
                writer.Write(new byte[2] { 0, 0 });
                writer.Write(anim.FramesPerSecond);
                writer.Write(frameCount + 1);
                writer.Write(boneIds.Count);
                writer.Write((byte)0);
                writer.Write(new byte[3] { 0, 0, 0 });
                writer.Write((uint)0);

                foreach (int boneId in boneIds) {
                    WriteCString(writer, OWMDLWriter.IdToString("bone", (uint)boneId));
                }

                foreach (int boneId in boneIds) {
                    Dictionary<AnimChannelID, SortedList<int, object>> dict = framesByBone[boneId];
                    writer.Write((byte)0);
                    if (everHas.HasFlag(SEAnimPresence.BoneLocation)) {
                        WriteFrames3d(writer, frameWidth, dict[AnimChannelID.POSITION]);
                    }
                    if (everHas.HasFlag(SEAnimPresence.BoneRotation)) {
                        WriteFrames4d(writer, frameWidth, dict[AnimChannelID.ROTATION]);
                    }
                    if (everHas.HasFlag(SEAnimPresence.BoneScale)) {
                        WriteFrames3d(writer, frameWidth, dict[AnimChannelID.SCALE]);
                    }
                }
            }
            return true;
        }

        public bool Write(Map10 physics, Stream output, object[] data) {
            return false;
        }

        public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, object[] data) {
            return false;
        }

        public Dictionary<ulong, List<string>>[] Write(Stream output, Map map, Map detail1, Map detail2, Map props, Map lights, string name = "") {
            return null;
        }
    }
}

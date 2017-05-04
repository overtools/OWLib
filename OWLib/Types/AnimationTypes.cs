using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace OWLib.Types {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AnimHeader {
        public uint priority;
        public float duration;
        public float fps;
        public ushort bonecount;
        public ushort flags;
        private ulong unk2;
        public ulong F08Key;
        private ulong padding;
        public ulong boneListOffset;
        public ulong infoTableOffset;
        private ulong size;
        private ulong eof;
        private ulong zero;
    }

    public struct AnimInfoTable {
        public ushort ScaleCount;
        public ushort PositionCount;
        public ushort RotationCount;
        public ushort flags;
        public int ScaleIndicesOffset;
        public int PositionIndicesOffset;
        public int RotationIndicesOffset;
        public int ScaleDataOffset;
        public int PositionDataOffset;
        public int RotationDataOffset;
    }

    [Flags]
    public enum AnimChannelID : byte {
        POSITION = 1,
        ROTATION = 2,
        SCALE = 4
    }

    public struct FrameValue {
        public AnimChannelID Channel;
        public object Value;

        public FrameValue(AnimChannelID a, object b) {
            Channel = a;
            Value = b;
        }
    }

    public struct BoneAnimation {
        public int BoneID;
        public List<FrameValue> Values;
    }

    public struct Keyframe {
        public float FramePosition;
        public int FramePositionI;
        public List<BoneAnimation> BoneFrames;
    }
}

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TankLib {
    /// <summary>Tank Animation, type 006</summary>
    public class teAnimation {
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct AnimHeader {
            public uint Priority;
            public float Duration;
            public float FPS;
            public ushort BoneCount;
            public ushort Flags;
            private ulong Unknown;
            public teResourceGUID Effect;
            private ulong Padding;
            public ulong BoneListOffset;
            public ulong InfoTableOffset;
            private ulong Size;
            private ulong Eof;
            private ulong Zero;
        }

        public AnimHeader Header;
        
        /// <summary>Load animation from a stream</summary>
        public teAnimation(Stream stream, bool keepOpen = false) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, keepOpen)) {
                Header = reader.Read<AnimHeader>();
            }
        }
    }
}
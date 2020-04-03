using System;
using System.Buffers.Binary;
using System.Text;

namespace ReplayMp4Tool {
    public class MP4Atom {
        public MP4Atom(Span<byte> buffer) {
            Size = BinaryPrimitives.ReadInt32BigEndian(buffer);
            if (Size < 8) return;
            Name = Encoding.ASCII.GetString(buffer.Slice(4, 4).ToArray());
            if (Size > 8) {
                Buffer = buffer.Slice(8, Size - 8).ToArray();
            }
        }
        
        public int Size { get; set; }
        public string Name { get; set; } = "";
        public Memory<byte> Buffer { get; set; }
    }
}

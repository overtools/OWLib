using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TankLib {
    /// <summary>Tank string</summary>
    public class teString {
        /// <summary>Value of the string</summary>
        public string Value;
        public teEnums.SDAM Mutability;

        public teString(string value) {
            Value = value;
            Mutability = teEnums.SDAM.NONE;
        }
        
        public teString(string value, teEnums.SDAM mutability) {
            Value = value;
            Mutability = mutability;
        }
        
        /// <summary>Header for 07C and 0A9 strings</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ArchiveStringHeader {
            public ulong Offset;
            public uint Unknown1;
            public uint References;
        }

        /// <summary>
        /// Load "ArchiveString" from a stream
        /// </summary>
        /// <param name="stream">The stream to load from</param>
        public teString(Stream stream) {
            if (stream == null) return;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8)) {
                ArchiveStringHeader header = reader.Read<ArchiveStringHeader>();
                stream.Position = (long)header.Offset;
                char[] bytes = reader.ReadChars((int)(stream.Length - stream.Position));

                Value = new string(bytes).TrimEnd('\0');
            }
        }

        public static implicit operator string(teString @string) {
            return @string.Value;
        }

        public override string ToString() {
            return Value;
        }
    }
}
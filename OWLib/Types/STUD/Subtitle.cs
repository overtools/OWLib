using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class Subtitle : ISTUDInstance {
        // Subtitle is instance 1 (remember that we're programmers here) in 071 files.
        public uint Id => 0x190D4773;
        public string Name => "Subtitle";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SubtitleData {
            public STUDInstanceInfo instance;
            public uint arrayInfoOffset;  // 96
            public uint unk2;  // 0
            public uint unk3;  // 0
            public uint unk4;  // 0
            public uint unk5;  // 0
            public uint unk6;  // 0
        }

        private SubtitleData data;
        private string str;

        public SubtitleData Data => data;
        public string String => str;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, true)) {
                data = reader.Read<SubtitleData>();

                input.Position = data.arrayInfoOffset;
                // seems to use nonstandard array, defined by two uints, with one uint inbetween
                uint count = reader.Read<uint>();
                reader.Read<uint>();  // unk7, seems to be apart of the array definiton
                uint offset = reader.Read<uint>();

                if (count > 0) {
                    input.Position = offset;
                    str = new string(reader.ReadChars((int)count));
                } else {
                    str = "";
                }
                
            }
        }
    }
}

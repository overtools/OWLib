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
            public uint unk1;  // 96
            public uint unk2;  // 0
            public uint unk3;  // 0
            public uint unk4;  // 0
            public uint unk5;  // 0
            public uint unk6;  // 0
            public uint count;
            public uint unk7;
            public uint offset;
        }

        private SubtitleData data;
        private string str;

        public SubtitleData Data => data;
        public string String => str;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, true)) {
                data = reader.Read<SubtitleData>();

                char[] chars = new char[data.count];

                if (data.count > 0) {
                    // seems to use nonstandard array, defined by two uints, with one uint inbetween
                    // see length, unk7, offset
                    input.Position = data.offset;
                    chars = reader.ReadChars((int)data.count);
                }
                str = new string(chars);
            }
        }
    }
}

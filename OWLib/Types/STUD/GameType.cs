using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class GameType : ISTUDInstance {
        public uint Id => 0x9A8B17B5;
        public string Name => "GameType";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct GameTypeHeader {
            public STUDInstanceInfo instance;
            public OWRecord raw_type;
            public ulonglong unknown1;
            public ulonglong unknown2;
            public ulonglong unknown3;
            public ulonglong unknown4;
            public OWRecord name;
            public ulong unknown;
        }

        private GameTypeHeader header;

        public GameTypeHeader Header => header;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<GameTypeHeader>();
            }
        }
    }
}

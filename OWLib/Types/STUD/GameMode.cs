using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class GameMode : ISTUDInstance {
        public uint Id => 0x9A8B17B5;
        public string Name => "GameMode";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct GameModeHeader {
            public STUDInstanceInfo instance;
            public OWRecord ruleset;
            public ulonglong unknown1;
            public ulonglong unknown2;
            public ulonglong unknown3;
            public ulonglong unknown4;
            public OWRecord name;
            public ulong unknown;
        }

        private GameModeHeader header;

        public GameModeHeader Header => header;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<GameModeHeader>();
            }
        }
    }
}

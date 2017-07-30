using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class GameTypeParam : ISTUDInstance {
        public uint Id => 0x0EBCCCEC;
        public string Name => "GameTypeParam";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct GameTypeParamHeader {
            public STUDInstanceInfo instance;
            public OWRecord unknown;
            public OWRecord name;
        }

        private GameTypeParamHeader header;

        public GameTypeParamHeader Header => header;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<GameTypeParamHeader>();
            }
        }
    }
}

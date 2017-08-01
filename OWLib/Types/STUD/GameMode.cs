using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
    [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
    public class GameMode : ISTUDInstance {
        public uint Id => 0xDCFCB3AC;
        public string Name => "GameMode";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct GameModeHeader {
            public STUDInstanceInfo instance;
            public ulonglong unknown_0;
            public ulonglong unknown_1;
            public ulonglong unknown_2;
            public ulonglong unknown_3;
            public ulong unknown_4;
            public OWRecord unknown_5;
            public OWRecord unknown_6;
            public OWRecord unknown_7;
            public OWRecord unknown_8;
            public ulonglong unknown_9;
            public ulonglong unknown_A;
            public Vec4 unknown_B;
            public ulong unknown_C;
            public OWRecord name;
            public OWRecord difficultyName;
            public OWRecord description;
            public OWRecord difficultyDescription;
            public ulonglong strings;
            public OWRecord icon;
            public OWRecord difficultyImage;
            public ulonglong @params;
            public OWRecord unknown4;
            public OWRecord unknown5;
            public OWRecord unknown6;
            public OWRecord unknown7;
            public OWRecord unknown8;
            public OWRecord unknown9; // ruleset most likely.
            public ulonglong types;
            public ulonglong unknownA;
            public OWRecord statistic;
        }

        private GameModeHeader header;
        private OWRecord[] strings;
        private OWRecord[] @params;
        private OWRecord[] types;

        public GameModeHeader Header => header;
        public OWRecord[] Strings => strings;
        public OWRecord[] Params => @params;
        public OWRecord[] Types => types;

        public void Read(Stream input, OWLib.STUD stud) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                header = reader.Read<GameModeHeader>();

                if (header.strings > 0) {
                    input.Position = header.strings;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    strings = new OWRecord[info.count];
                    input.Position = (long)info.offset;
                    for (uint i = 0; i < info.count; ++i) {
                        strings[i] = reader.Read<OWRecord>();
                    }
                } else {
                    strings = new OWRecord[0];
                }

                if (header.@params > 0) {
                    input.Position = header.@params;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    @params = new OWRecord[info.count];
                    input.Position = (long)info.offset;
                    for (uint i = 0; i < info.count; ++i) {
                        @params[i] = reader.Read<OWRecord>();
                    }
                } else {
                    @params = new OWRecord[0];
                }

                if (header.types > 0) {
                    input.Position = header.types;
                    STUDArrayInfo info = reader.Read<STUDArrayInfo>();
                    types = new OWRecord[info.count];
                    input.Position = (long)info.offset;
                    for (uint i = 0; i < info.count; ++i) {
                        types[i] = reader.Read<OWRecord>();
                    }
                } else {
                    types = new OWRecord[0];
                }
            }
        }
    }
}

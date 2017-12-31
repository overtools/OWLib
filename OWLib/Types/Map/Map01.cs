using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class Map01 : IMapFormat {
        public bool HasSTUD => false;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map01Header {
            public ulong Model;
            public uint groupCount;
            public uint totalRecordCount;
            public uint mask;
            public uint unk1;
            public uint unk2;
            public uint unk3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map01Group {
            public ulong ModelLook;
            public uint size;
            public uint recordCount;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map01GroupRecord {
            public MapVec3 position;
            public MapVec3 scale;
            public MapQuat rotation;
            public uint unk1;
            public uint unk2;
            public uint unk3;
            public uint unk4;
            public uint unk5;
            public uint unk6;
            public uint unk7;
            public int unk8;
            public int unk9;
            public int unkA;
            public int unkB;
            public int unkC;
        }

        public ushort Identifier => 1;

        public string Name => "Object Placement";

        private Map01Header header;
        private Map01Group[] groups;
        private Map01GroupRecord[][] records;

        public Map01Header Header => header;
        public Map01Group[] Groups => groups;
        public Map01GroupRecord[][] Records => records;

        public void Read(Stream data) {
            using(BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                header = reader.Read<Map01Header>();
                groups = new Map01Group[header.groupCount];
                records = new Map01GroupRecord[header.groupCount][];
                for(uint i = 0; i < header.groupCount; ++i) {
                    groups[i] = reader.Read<Map01Group>();
                    records[i] = new Map01GroupRecord[groups[i].recordCount];
                    for(uint j = 0; j < groups[i].recordCount; ++j) {
                        records[i][j] = reader.Read<Map01GroupRecord>();
                    }
                }
            }
        }
    }
}

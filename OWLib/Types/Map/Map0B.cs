using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class Map0B : IMapFormat {
        public bool HasSTUD => true;

        public ushort Identifier => 0xB;

        public string Name => "Prop Models";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct Map0BHeader {
            public ulong binding;
            public ulong unk0;
            public ulong unk1;
            public MapVec3 position;
            public MapVec3 scale;
            public MapQuat rotation;
            public uint extraCount;
            public fixed uint unk2[15];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map0BExtra {
            public uint unk1;
            public uint unk2;
        }
    
        private Map0BHeader header;
        public Map0BHeader Header => header;
        private Map0BExtra[] extra;
        public Map0BExtra[] Extra => extra;

        public ulong Model = 0;
        public ulong ModelLook = 0;

        public void Read(Stream data) {
            using(BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                header = reader.Read<Map0BHeader>();

                extra = new Map0BExtra[header.extraCount];
                for(uint i = 0; i < header.extraCount; ++i) {
                    extra[i] = reader.Read<Map0BExtra>();
                }
            }
        }
    }
}

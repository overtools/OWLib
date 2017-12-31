using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class Map08 : IMapFormat {
        public bool HasSTUD => false;

        public ushort Identifier => 8;

        public string Name => "Detail Models";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map08Header {
            public ulong Model;
            public ulong ModelLook;
            public MapVec3 position;
            public MapVec3 scale;
            public MapQuat rotation;
            public MapQuat unk;
        }

        private Map08Header header;
        public Map08Header Header => header;

        public void Read(Stream data) {
            using(BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                header = reader.Read<Map08Header>();
            }
        }
    }
}

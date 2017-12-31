using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class Map02 : IMapFormat {
        public bool HasSTUD => false;

        public ushort Identifier => 2;

        public string Name => "Detail Models A";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map02Header {
            public ulong Model;
            public ulong ModelLook;
            public MapVec3 position;
            public MapVec3 scale;
            public MapQuat rotation;
            public MapQuat unk;
        }

        private Map02Header header;
        public Map02Header Header => header;

        public void Read(Stream data) {
            using(BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                header = reader.Read<Map02Header>();
            }
        }
    }
}

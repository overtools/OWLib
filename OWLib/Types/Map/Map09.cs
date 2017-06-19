using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class Map09 : IMapFormat {
        public bool HasSTUD => false;

        public ushort Identifier => 9;

        public string Name => "Lights";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Map09Header {
            public MapQuat rotation;
            public MapVec3 position;
            public uint unknown1A;
            public uint unknown1B;
            public byte unknown2A;
            public byte unknown2B;
            public byte unknown2C;
            public byte unknown2D;
            public uint unknown3A;
            public uint unknown3B;
            public uint LightType;      // 1 = Spot Light, 2 = Point Light, 0 = Directional?
            public MapVec3 Color;
            public MapVec3 unknownPos1;
            public MapQuat unknownQuat1;
            public float LightFOV;      // Cone angle, in degrees. Set to -1.0 for Point Lights
            public MapVec3 unknownPos2;
            public MapQuat unknownQuat2;
            public MapVec3 unknownPos3;
            public MapQuat unknownQuat3;
            public float unknown4A;
            public float unknown4B;
            public uint unknown5;       // definitly an Int of some kind
            public short unknown6A;
            public short unknown6B;
            public uint unknown7A;
            public uint unknown7B;

        }

        private Map09Header header;
        public Map09Header Header => header;

        public void Read(Stream data) {
            using (BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                header = reader.Read<Map09Header>();
            }
        }
    }
}

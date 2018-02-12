using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Map {
    public class MapEntity : IMapFormat {
        public bool HasSTUD => true;

        public ushort Identifier => 0xB;

        public string Name => "Entity";

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public unsafe struct MapEntityHeader {
            public ulong Entity;
            public ulong Unknown1;
            public ulong Unknown2;
            public MapVec3 Position;
            public MapVec3 Scale;
            public MapQuat Rotation;
            public uint STUBindingCount;
            public fixed uint Unknown3[15];
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MapEntitySTUBinding {
            public long Unknown;
        }

        public MapEntityHeader Header { get; private set; }
        public MapEntitySTUBinding[] STUBindings { get; private set; }
        public List<object> STUContainers;  // wtf no
        
        // todo: dump these
        public ulong Model = 0;
        public ulong ModelLook = 0;

        public void Read(Stream data) {
            using(BinaryReader reader = new BinaryReader(data, System.Text.Encoding.Default, true)) {
                Header = reader.Read<MapEntityHeader>();

                STUBindings = new MapEntitySTUBinding[Header.STUBindingCount];
                for(uint i = 0; i < Header.STUBindingCount; ++i) {
                    STUBindings[i] = reader.Read<MapEntitySTUBinding>();
                }
            }
        }
    }
}

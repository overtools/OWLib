using System.IO;
using System.Runtime.InteropServices;
using TankLib.Math;

namespace TankLib.Chunks {
    /// <inheritdoc />
    /// <summary>MHRP: Defines hardpoints for a model</summary>
    public class teModelChunk_Hardpoint : IChunk {
        public string ID => "MHRP";
        
        /// <summary>MHRP header</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HardpointHeader {
            public int HardpointCount;
            public int UnknownCount;
            public long HardpointOffset;
            public long UnknownOffset;
        }
        
        /// <summary>A single model hardpoint</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Hardpoint {
            public teMtx44A Matrix;
            public teResourceGUID GUID;
            public teResourceGUID ParentBone;

            public ulong Unknown1;
            public ulong Unknown2;
        }

        /// <summary>Header data</summary>
        public HardpointHeader Header;
        
        /// <summary>Hardpoint definitions</summary>
        public Hardpoint[] Hardpoints;
        
        /// <summary>An unknown byte array after the hardpoint definitions</summary>
        public byte[] Unknown;

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<HardpointHeader>();
                
                if (Header.HardpointOffset > 0) {
                    Hardpoints = new Hardpoint[Header.HardpointCount];
                    input.Position = Header.HardpointOffset;
                    for (uint i = 0; i < Header.HardpointCount; ++i) {
                        Hardpoints[i] = reader.Read<Hardpoint>();
                    }
                }

                if (Header.UnknownOffset > 0) {
                    Unknown = new byte[Header.UnknownCount];
                    input.Position = Header.UnknownOffset;

                    for (int i = 0; i < Header.UnknownCount; i++) {
                        Unknown[i] = reader.ReadByte();
                    }
                }
            }
        }
    }
}
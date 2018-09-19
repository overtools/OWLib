using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Material Data, file type 0B3</summary>
    public class teMaterialData {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatDataHeader {
            /// <summary>Offset to static input definitions</summary>
            public long StaticInputsOffset;  // 0
            public long Offset2;  // 8
            
            /// <summary>Offset to texture definitions</summary>
            public long TextureOffset;  // 16
            
            public long Offset4;  // 24
            
            public uint Unknown1;  // 28
            
            /// <summary>Number of static inputs </summary>
            public ushort StaticInputCount; // 32
            
            public ushort Unknown3;
            
            /// <summary>Texture definition count</summary>
            public byte TextureCount;
            
            public byte Offset4Count;
            public ushort Unknown4;
            public uint Unknown5;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Unknown {
            public ulong A;
            public ulong B;
        }

        /// <summary>MaterialData Texture</summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Texture {
            /// <summary>Texture GUID</summary>
            /// <remarks>File type 004</remarks>
            public teResourceGUID TextureGUID;
        
            /// <summary>CRC32 of input name</summary>
            /// <remarks>Matches up on teShaderInstance</remarks>
            public uint NameHash;
        
            /// <summary>Unknown flags</summary>
            public byte Flags;
        }

        /// <summary>Header data</summary>
        public MatDataHeader Header;
        
        /// <summary>Texture definitions</summary>
        public Texture[] Textures;
        
        /// <summary>Unknown definitions</summary>
        public Unknown[] Unknowns;

        /// <summary>Constant buffer definitions</summary>
        public teMaterialDataStaticInput[] StaticInputs;

        /// <summary>Load material data from a stream</summary>
        public teMaterialData(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<MatDataHeader>();

                if (Header.TextureOffset > 0) {
                    reader.BaseStream.Position = Header.TextureOffset;

                    Textures = reader.ReadArray<Texture>(Header.TextureCount);
                }

                if (Header.Offset4 > 0) {
                    reader.BaseStream.Position = Header.Offset4;

                    Unknowns = reader.ReadArray<Unknown>(Header.Offset4Count);
                }

                if (Header.StaticInputsOffset > 0) {
                    reader.BaseStream.Position = Header.StaticInputsOffset;
                    StaticInputs = new teMaterialDataStaticInput[Header.StaticInputCount];
                    for (int i = 0; i < Header.StaticInputCount; i++) {
                        StaticInputs[i] = new teMaterialDataStaticInput(reader);
                    }
                }
            }
        }

        public Texture GetTexture(uint hash) {
            if (Textures == null) return default;
            foreach (Texture texture in Textures) {
                if (texture.NameHash == hash) {
                    return texture;
                }
            }
            return default;
        }
    }

    public class teMaterialDataStaticInput {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct HeaderData {
            public uint Hash;
            public Type Flags;
            public byte Count;
            public short Unknown;
        }

        public enum Type : byte {
            
        }

        public static readonly int[] Sizes = {0, 4, 4, 4, 4, 4, 4, 8, 8, 0xC, 0x10, 0x10, 0x24, 0x30};

        public HeaderData Header;
        public byte[] Data;

        public teMaterialDataStaticInput(BinaryReader reader) {
            Header = reader.Read<HeaderData>();
            
            byte idx = (byte) Header.Flags;
            Data = reader.ReadBytes(Header.Count * Sizes[idx]);
        }
    }
}
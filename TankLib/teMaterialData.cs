using System;
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

        /// <summary>Header data</summary>
        public MatDataHeader Header;
        
        /// <summary>Texture definitions</summary>
        public teMaterialDataTexture[] Textures;
        
        /// <summary>Unknown definitions</summary>
        public teMaterialDataUnknown[] Unknowns;

        /// <summary>Constant buffer definitions</summary>
        public teMaterialDataStaticInput[] StaticInputs;

        /// <summary>Load material data from a stream</summary>
        public teMaterialData(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<MatDataHeader>();

                if (Header.TextureOffset > 0) {
                    reader.BaseStream.Position = Header.TextureOffset;
                    
                    Textures = reader.ReadArray<teMaterialDataTexture>(Header.TextureCount);
                }

                if (Header.Offset4 > 0) {
                    reader.BaseStream.Position = Header.Offset4;

                    Unknowns = reader.ReadArray<teMaterialDataUnknown>(Header.Offset4Count);
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

        public teMaterialDataTexture GetTexture(uint hash) {
            if (Textures == null) return default;
            foreach (teMaterialDataTexture texture in Textures) {
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
            public TestByteFlags Flags;
            public byte Size;
            public short Unknown;
        }

        public HeaderData Header;
        public byte[] Data;

        public teMaterialDataStaticInput(BinaryReader reader) {
            Header = reader.Read<HeaderData>();
            int size = Header.Size;
            byte intFlags = (byte) Header.Flags;

            // todo: actually use the damn flags
            if (intFlags == 10) { // F00000002, F00000008
                size *= 16;
            } else if (intFlags == 8) { // F00000008
                size *= 8;
            } else if (intFlags == 11) { // F00000001, F00000002, F00000008
                size *= 16;
            } else if (intFlags == 3) { // F00000001, F00000002
                size *= 4;
            } else if (intFlags == 2) { // F00000002
                size *= 4;
            } else if (intFlags == 6) { // F00000002, F00000004
                size *= 4;
            } else if (intFlags == 9) { // F00000001, F00000008
                size *= 12;
            } else {
                throw new Exception($"teMaterialDataStaticInput: Unsure how much to read for data ({intFlags}, flags: {Header.Flags}, offset: {reader.BaseStream.Position})");
            }

            Data = reader.ReadBytes(size);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMaterialDataUnknown {
        public ulong A;
        public ulong B;
    }

    /// <summary>MaterialData Texture</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMaterialDataTexture {
        /// <summary>Texture GUID</summary>
        /// <remarks>File type 004</remarks>
        public teResourceGUID Texture;
        
        /// <summary>CRC32 of input name</summary>
        /// <remarks>Matches up on teShaderInstance</remarks>
        public uint NameHash;
        
        /// <summary>Unknown flags</summary>
        public byte Flags;
    }
}
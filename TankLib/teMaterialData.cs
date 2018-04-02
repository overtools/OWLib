using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Material Data, file type 0B3</summary>
    public class teMaterialData {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatDataHeader {
            public long Offset1;
            public long Offset2;
            
            /// <summary>Texture definition offset</summary>
            public long TextureOffset;
            public long Offset4;
            public uint unk1;
            public ushort unk2;
            public ushort unk3;
            
            /// <summary>Texture definition count</summary>
            public byte TextureCount;
            
            public byte Offset4Count;
            public ushort unk4;
            public uint unk5;
        }

        /// <summary>Header data</summary>
        public MatDataHeader Header;
        
        /// <summary>Texture definitions</summary>
        public teMaterialDataTexture[] Textures;
        
        /// <summary>Unknown definitions</summary>
        public teMaterialDataUnknown[] Unknowns;

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
            }
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
        
        /// <summary>Shader input type</summary>
        /// <remarks>Matches up on teShaderInstance</remarks>
        public teShaderTextureType Type;
        
        /// <summary>Unknown flags</summary>
        public byte Flags;
    }

    public enum teShaderTextureType : uint {
        None = 0,
        DiffuseAO = 2903569922  // Alpha channel is AO
    }
}
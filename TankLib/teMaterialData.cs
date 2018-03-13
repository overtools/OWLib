using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Material Data, file type 0B3</summary>
    public class teMaterialData {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct MatDataHeader {
            public long Offset1;
            public long Offset2;
            public long TextureOffset;
            public long Offset4;
            public uint unk1;
            public ushort unk2;
            public ushort unk3;
            public byte TextureCount;
            public byte Offset4Count;
            public ushort unk4;
            public uint unk5;
        }

        public MatDataHeader Header;
        public teMaterialDataTexture[] Textures;

        /// <summary>Load material data from a stream</summary>
        public teMaterialData(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Header = reader.Read<MatDataHeader>();

                if (Header.TextureOffset > 0) {
                    reader.BaseStream.Position = Header.TextureOffset;
                    
                    Textures = new teMaterialDataTexture[Header.TextureCount];

                    for (int i = 0; i < Header.TextureCount; i++) {
                        Textures[i] = reader.Read<teMaterialDataTexture>();
                    }
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teMaterialDataTexture {
        public teResourceGUID Texture;
        public teShaderTextureType Type;
        public byte Flags;
    }

    public enum teShaderTextureType : uint {
        None = 0,
        DiffuseAO = 2903569922  // Alpha channel is AO
    }
}
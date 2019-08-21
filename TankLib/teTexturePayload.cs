using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank Texture Payload, type 04D</summary>
    public class teTexturePayload {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct TexturePayloadHeader {
            public uint Mips;
            public uint Surfaces;
            public uint ImageSize;
            public uint HeaderSize;
        }
        
        public TexturePayloadHeader Header;
        public uint Size;
        public uint[] Color1;
        public uint[] Color2;
        public ushort[] Color3;
        public ushort[] Color4;
        public uint[] Color5;
        public byte[] RawData;

        /// <summary>Load payload from the parent texture + payload stream</summary>
        /// <param name="parent">Parent texture object</param>
        /// <param name="payloadStream">Stream to load from</param>
        public teTexturePayload(teTexture parent, Stream payloadStream) {
            using (BinaryReader dataReader = new BinaryReader(payloadStream)) {
                Header = dataReader.Read<TexturePayloadHeader>();

                if(parent.Header.GetTextureType() == TextureTypes.TextureType.Unknown) {
                    RawData = dataReader.ReadBytes((int)Header.ImageSize);
                    return;
                }

                Size = Header.ImageSize / parent.Header.GetTextureType().ByteSize();
                Color1 = new uint[Size];
                Color2 = new uint[Size];
                Color3 = new ushort[Size];
                Color4 = new ushort[Size];
                Color5 = new uint[Size];

                if (parent.Header.Format > 72) {
                    Color3 = dataReader.ReadArray<ushort>((int)Size);
                    
                    for (int i = 0; i < Size; ++i) {  // todo: can make this faster
                        Color4[i] = dataReader.ReadUInt16();
                        Color5[i] = dataReader.ReadUInt32();
                    }
                }

                if (parent.Header.Format < 80) {
                    Color1 = dataReader.ReadArray<uint>((int)Size);
                    Color2 = dataReader.ReadArray<uint>((int)Size);
                }
            }
        }

        /// <summary>Save DDS to stream</summary>
        /// <param name="parentHeader">Parent teTexture header</param>
        /// <param name="ddsWriter">Stream to be written to</param>
        /// <param name="mips"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="surfaces"></param>
        public void SaveToDDS(teTexture.TextureHeader parentHeader, BinaryWriter ddsWriter, int? mips, uint? width, uint? height, uint? surfaces) {
            TextureTypes.DDSHeader dds = parentHeader.ToDDSHeader(mips ?? parentHeader.Mips, width ?? parentHeader.Width, height ?? parentHeader.Height, surfaces ?? parentHeader.Surfaces);
            ddsWriter.Write(dds);
            if (dds.Format.FourCC == (int) TextureTypes.TextureType.Unknown) {
                TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                    Format = parentHeader.Format,
                    Dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D,
                    Misc = (uint) (parentHeader.IsCubemap ? 0x4 : 0),
                    Size = (uint) (parentHeader.IsCubemap ? 1 : (surfaces ?? parentHeader.Surfaces)),
                    Misc2 = 0
                };
                ddsWriter.Write(d10);
            }

            SaveToDDSData(parentHeader, ddsWriter);
        }

        /// <summary>Save DDS data to stream</summary>
        /// <param name="parentHeader">Parent teTexture header</param>
        /// <param name="ddsWriter">Stream to be written to</param>
        public void SaveToDDSData(teTexture.TextureHeader parentHeader, BinaryWriter ddsWriter)
        {
            if (RawData != null)
            {
                ddsWriter.BaseStream.Write(RawData, 0, (int)Header.ImageSize);
                return;
            }
            for (int i = 0; i < Size; ++i)
            {
                if (parentHeader.Format > 72)
                {
                    ddsWriter.Write(Color3[i]);
                    ddsWriter.Write(Color4[i]);
                    ddsWriter.Write(Color5[i]);
                }

                if (parentHeader.Format < 80)
                {
                    ddsWriter.Write(Color1[i]);
                    ddsWriter.Write(Color2[i]);
                }
            }
        }
    }
}
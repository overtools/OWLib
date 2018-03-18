using System.IO;
using System.Text;

namespace TankLib {
    /// <summary>Tank Texture Payload, type 04D</summary>
    public class teTexturePayload {
        public TextureTypes.TexturePayloadHeader RawHeader;
        public TextureTypes.TextureType Format;
        public uint Size;
        public uint[] Color1;
        public uint[] Color2;
        public ushort[] Color3;
        public ushort[] Color4;
        public uint[] Color5;

        /// <summary>Parent texture object</summary>
        public teTexture Header;

        /// <summary>Load payload from the parent texture + payload stream</summary>
        /// <param name="header">Parent texture object</param>
        /// <param name="payloadStream">Stream to load from</param>
        public teTexturePayload(teTexture header, Stream payloadStream) {
            Header = header;
            using (BinaryReader dataReader = new BinaryReader(payloadStream)) {
                RawHeader = dataReader.Read<TextureTypes.TexturePayloadHeader>();

                Size = RawHeader.ImageSize / header.Header.GetTextureType().ByteSize();
                Color1 = new uint[Size];
                Color2 = new uint[Size];
                Color3 = new ushort[Size];
                Color4 = new ushort[Size];
                Color5 = new uint[Size];

                if ((byte) header.Header.Format > 72) {
                    for (int i = 0; i < Size; ++i) {
                        Color3[i] = dataReader.ReadUInt16();
                    }
                    for (int i = 0; i < Size; ++i) {
                        Color4[i] = dataReader.ReadUInt16();
                        Color5[i] = dataReader.ReadUInt32();
                    }
                }

                if ((byte) header.Header.Format < 80) {
                    for (int i = 0; i < Size; ++i) {
                        Color1[i] = dataReader.ReadUInt32();
                    }
                    for (int i = 0; i < Size; ++i) {
                        Color2[i] = dataReader.ReadUInt32();
                    }
                }
            }
        }

        /// <summary>Save DDS to stream</summary>
        /// <param name="stream">Stream to be written to</param>
        /// <param name="keepOpen">Keep the stream open after writing</param>
        public void SaveToDDS(Stream stream, bool keepOpen=false) {
            using (BinaryWriter ddsWriter = new BinaryWriter(stream, Encoding.Default, keepOpen)) {
                TextureTypes.DDSHeader dds = Header.Header.ToDDSHeader();
                ddsWriter.Write(dds);
                if (dds.Format.FourCC == 808540228) {
                    TextureTypes.DDS_HEADER_DXT10 d10 = new TextureTypes.DDS_HEADER_DXT10 {
                        Format = (uint)Header.Header.Format,
                        Dimension = TextureTypes.D3D10_RESOURCE_DIMENSION.TEXTURE2D,
                        Misc = (uint)(Header.Header.IsCubemap() ? 0x4 : 0),
                        Size = (uint)(Header.Header.IsCubemap() ? 1 : Header.Header.Surfaces),
                        Misc2 = 0
                    };
                    ddsWriter.Write(d10);
                }
                for (int i = 0; i < Size; ++i) {
                    if ((byte)Header.Header.Format > 72) {
                        ddsWriter.Write(Color3[i]);
                        ddsWriter.Write(Color4[i]);
                        ddsWriter.Write(Color5[i]);
                    }

                    if ((byte)Header.Header.Format < 80) {
                        ddsWriter.Write(Color1[i]);
                        ddsWriter.Write(Color2[i]);
                    }
                }
            }
        }
        
        /// <summary>Save DDS to stream</summary>
        /// <param name="keepOpen">Keep the stream open after writing</param>
        public Stream SaveToDDS(bool keepOpen=false) {
            MemoryStream stream = new MemoryStream();
            SaveToDDS(stream, keepOpen);
            return stream;
        }
    }
}
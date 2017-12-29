using System.IO;
using OWLib.Types;

namespace OWLib {
    public class Texture {
        public TextureHeader Header { get; }
        public RawTextureHeader RawHeader { get; }
        public TextureType Format { get; }
        public uint Size { get; }
        public uint[] Color1 { get; }
        public uint[] Color2 { get; }
        public ushort[] Color3 { get; }
        public ushort[] Color4 { get; }
        public uint[] Color5 { get; }
        public bool Loaded { get; } = false;

        public Texture(Stream headerStream, Stream dataStream) {           
            using (BinaryReader headerReader = new BinaryReader(headerStream)) {
                Header = headerReader.Read<TextureHeader>();
                if (Header.dataSize != 0) {
                    return;
                }

                Format = Header.Format();

                if (Format == TextureType.Unknown) {
                    return;
                }

                using (BinaryReader dataReader = new BinaryReader(dataStream)) {
                    RawHeader = dataReader.Read<RawTextureHeader>();

                    Size = RawHeader.imageSize / Header.Format().ByteSize();
                    Color1 = new uint[Size];
                    Color2 = new uint[Size];
                    Color3 = new ushort[Size];
                    Color4 = new ushort[Size];
                    Color5 = new uint[Size];

                    if ((byte) Header.format > 72) {
                        for (int i = 0; i < Size; ++i) {
                            Color3[i] = dataReader.ReadUInt16();
                        }
                        for (int i = 0; i < Size; ++i) {
                            Color4[i] = dataReader.ReadUInt16();
                            Color5[i] = dataReader.ReadUInt32();
                        }
                    }

                    if ((byte) Header.format < 80) {
                        for (int i = 0; i < Size; ++i) {
                            Color1[i] = dataReader.ReadUInt32();
                        }
                        for (int i = 0; i < Size; ++i) {
                            Color2[i] = dataReader.ReadUInt32();
                        }
                    }
                }
                Loaded = true;
            }
        }

        public void Save(Stream ddsStream, bool keepOpen = false) {
            if (!Loaded) {
                return;
            }
            using (BinaryWriter ddsWriter = new BinaryWriter(ddsStream, System.Text.Encoding.Default, keepOpen)) {
                DDSHeader dds = Header.ToDDSHeader();
                ddsWriter.Write(dds);
                if (dds.format.fourCC == 808540228) {
                    DDS_HEADER_DXT10 d10 = new DDS_HEADER_DXT10 {
                        format = (uint)Header.format,
                        dimension = D3D10_RESOURCE_DIMENSION.TEXTURE2D,
                        misc = (uint)(Header.IsCubemap() ? 0x4 : 0),
                        size = (uint)(Header.IsCubemap() ? 1 : Header.surfaces),
                        misc2 = 0
                    };
                    ddsWriter.Write(d10);
                }
                for (int i = 0; i < Size; ++i) {
                    if ((byte)Header.format > 72) {
                        ddsWriter.Write(Color3[i]);
                        ddsWriter.Write(Color4[i]);
                        ddsWriter.Write(Color5[i]);
                    }

                    if ((byte)Header.format < 80) {
                        ddsWriter.Write(Color1[i]);
                        ddsWriter.Write(Color2[i]);
                    }
                }
            }
        }

        public Stream Save() {
            Stream ms = new MemoryStream();
            Save(ms, true);
            return ms;
        }
    }
}

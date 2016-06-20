using System.IO;
using OWLib.Types;

namespace OWLib {
  public class Texture {
    private TextureHeader header;
    private RawTextureHeader rawHeader;
    private TextureType format;
    private uint size;
    private uint[] color1;
    private uint[] color2;
    private ushort[] color3;
    private ushort[] color4;
    private uint[] color5;
    private bool loaded = false;

    public TextureHeader Header => header;
    public RawTextureHeader RawHeader => rawHeader;
    public TextureType Format => format;
    public uint Size => size;
    public uint[] Color1 => color1;
    public uint[] Color2 => color2;
    public ushort[] Color3 => color3;
    public ushort[] Color4 => color4;
    public uint[] Color5 => color5;
    public bool Loaded => loaded;

    public Texture(Stream headerStream, Stream dataStream) {
      using(BinaryReader headerReader = new BinaryReader(headerStream))
      using(BinaryReader dataReader = new BinaryReader(dataStream)) {
        header = headerReader.Read<TextureHeader>();
        if(header.dataSize != 0) {
          return;
        }

        format = header.Format();

        if(format == TextureType.Unknown) {
          return;
        }

        rawHeader = dataReader.Read<RawTextureHeader>();

        size   = rawHeader.imageSize / header.Format().ByteSize();
        color1 = new uint[size];
        color2 = new uint[size];
        color3 = new ushort[size];
        color4 = new ushort[size];
        color5 = new uint[size];

        if((byte)header.format > 72) {
          for(int i = 0; i < size; ++i) {
            color3[i] = dataReader.ReadUInt16();
          }
          for(int i = 0; i < size; ++i) {
            color4[i] = dataReader.ReadUInt16();
            color5[i] = dataReader.ReadUInt32();
          }
        }

        if((byte)header.format < 80) {
          for(int i = 0; i < size; ++i) {
            color1[i] = dataReader.ReadUInt32();
          }
          for(int i = 0; i < size; ++i) {
            color2[i] = dataReader.ReadUInt32();
          }
        }
      }
      loaded = true;
    }

    public void Save(Stream ddsStream) {
      if(!loaded) {
        return;
      }
      using(BinaryWriter ddsWriter = new BinaryWriter(ddsStream)) {
        DDSHeader dds = header.ToDDSHeader();
        ddsWriter.Write(dds);
        if(dds.format.fourCC == 808540228) {
          DDS_HEADER_DXT10 d10 = new DDS_HEADER_DXT10 {
            format = (uint)header.format,
            dimension = D3D10_RESOURCE_DIMENSION.TEXTURE2D,
            misc = (uint)(header.IsCubemap() ? 0x4 : 0),
            size = (uint)(header.IsCubemap() ? 1 : header.surfaces),
            misc2 = 0
          };
          ddsWriter.Write(d10);
        }
        for(int i = 0; i < size; ++i) {
          if((byte)header.format > 72) {
            ddsWriter.Write(color3[i]);
            ddsWriter.Write(color4[i]);
            ddsWriter.Write(color5[i]);
          }

          if((byte)header.format < 80) {
            ddsWriter.Write(color1[i]);
            ddsWriter.Write(color2[i]);
          }
        }
      }
    }

    public Stream Save() {
      Stream ms = new MemoryStream();
      Save(ms);
      return ms;
    }
  }
}

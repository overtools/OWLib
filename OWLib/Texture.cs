using System.IO;

namespace OWLib {
  public class Texture {
    private TextureHeader header;
    private RawTextureheader rawHeader;
    private TextureType format;
    private uint size;
    private uint[] color1;
    private uint[] color2;
    private ushort[] color3;
    private ushort[] color4;
    private uint[] color5;

    public TextureHeader Header => header;
    public RawTextureheader RawHeader => rawHeader;
    public TextureType Format => format;
    public uint Size => size;
    public uint[] Color1 => color1;
    public uint[] Color2 => color2;
    public ushort[] Color3 => color3;
    public ushort[] Color4 => color4;
    public uint[] Color5 => color5;

    public Texture(Stream headerStream, Stream dataStream) {
      headerStream.Position = 0;
      dataStream.Position = 0;
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

        rawHeader = dataReader.Read<RawTextureheader>();

        size   = rawHeader.imageSize / header.Format().ByteSize();
        color1 = new uint[size];
        color2 = new uint[size];
        color3 = new ushort[size];
        color4 = new ushort[size];
        color5 = new uint[size];

        if(header.format > 72) {
          for(int j = 0; j < size; j++) {
            color3[j] = dataReader.ReadUInt16();
          }
          for(int k = 0; k < size; k++) {
            color4[k] = dataReader.ReadUInt16();
            color5[k] = dataReader.ReadUInt32();
          }
        }

        if(header.format < 80) {
          for(int l = 0; l < size; l++) {
            color1[l] = dataReader.ReadUInt32();
          }
          for(int m = 0; m < size; m++) {
            color2[m] = dataReader.ReadUInt32();
          }
        }
      }
    }

    public void ToDDS(Stream ddsStream) {
      using(BinaryWriter ddsWriter = new BinaryWriter(ddsStream)) {
        DDSHeader dds = header.ToDDSHeader();
        dds.linearSize = rawHeader.imageSize;

        ddsWriter.Write(dds);
        for(int n = 0; n < size; n++) {
          if(header.format > 72) {
            ddsWriter.Write(color3[n]);
            ddsWriter.Write(color4[n]);
            ddsWriter.Write(color5[n]);
          }

          if(header.format < 80) {
            ddsWriter.Write(color1[n]);
            ddsWriter.Write(color2[n]);
          }
        }
      }
    }

    public Stream ToDDS() {
      Stream ms = new MemoryStream();
      ToDDS(ms);
      return ms;
    }
  }
}

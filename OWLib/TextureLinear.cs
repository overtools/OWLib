using System.IO;
using OWLib.Types;

namespace OWLib {
  public class TextureLinear {
    private TextureHeader header;
    private DXGI_PIXEL_FORMAT format;
    private uint size;
    private bool loaded = false;
    private byte[] data;

    public TextureHeader Header => header;
    public DXGI_PIXEL_FORMAT Format => format;
    public uint Size => size;
    public byte[] Data => data;
    public bool Loaded => loaded;

    public void Save(Stream output) {
      if(!loaded) {
        return;
      }
      using(BinaryWriter ddsWriter = new BinaryWriter(output)) {
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
        ddsWriter.Write(data, 0, (int)header.dataSize);
      }
    }

    public TextureLinear(Stream imageStream, bool keepOpen = false) {
      using(BinaryReader imageReader = new BinaryReader(imageStream, System.Text.Encoding.Default, keepOpen)) {
        header = imageReader.Read<TextureHeader>();
        size = header.dataSize;
        format = header.format;

        if(header.dataSize == 0) {
          return;
        }
        
        imageStream.Seek(128, SeekOrigin.Begin);
        data = new byte[header.dataSize];
        imageStream.Read(data, 0, (int)header.dataSize);
      }
      loaded = true;
    }
  }
}

using System.IO;
using System.IO.Compression;
using OWLib.Types;

namespace OWLib {
  public class DXBC {
    private DXBCHeader header;
    public DXBCHeader Header => header;

    private Stream data;
    public Stream Data => data;

    public DXBC(Stream input) {
      using(BinaryReader read = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = read.Read<DXBCHeader>();
        if(header.uncompressedSize > 0 && header.compressedSize > 0 && header.offset == 40) {
          input.Position = (long)header.offset;
          using(GZipStream gzip = new GZipStream(input, CompressionMode.Decompress)) {
            data = new MemoryStream();
            gzip.CopyTo(data);
            data.Position = 0;
          }
        }
      }
    }
  }

  public class DXBCSecondary {
    private DXBCSecondaryHeader header;
    public DXBCSecondaryHeader Header => header;

    private Stream data;
    public Stream Data => data;

    public DXBCSecondary(Stream input) {
      using(BinaryReader read = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = read.Read<DXBCSecondaryHeader>();
        if(header.uncompressedSize > 0 && header.compressedSize > 0 && header.offset == 48) {
          input.Position = (long)header.offset;
          using(GZipStream gzip = new GZipStream(input, CompressionMode.Decompress)) {
            data = new MemoryStream();
            gzip.CopyTo(data);
            data.Position = 0;
          }
        }
      }
    }
  }
}

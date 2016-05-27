using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OWLib;

namespace TextureTool {
  class Program {
    static void Main(string[] args) {
      if(args.Length < 3) {
        Console.Out.WriteLine("Usage: TextureTool.exe 004_file 04D_file output_file");
        return;
      }

      string headerFile = args[0];
      string dataFile = args[1];
      string destFile = args[2];

      using(Stream headerStream = File.Open(headerFile, FileMode.Open, FileAccess.Read))
      using(BinaryReader headerReader = new BinaryReader(headerStream)) {
        TextureHeader header = headerReader.Read<TextureHeader>();

        if(header.dataSize != 0) {
          Console.Error.WriteLine("Unsupported");
          return;
        }

        TextureType format = header.Format();

        if(format == TextureType.Unknown) {
          Console.Error.WriteLine("Unsupported format {0}", header.format);
          return;
        }

        using(Stream dataStream = File.Open(dataFile, FileMode.Open, FileAccess.Read))
        using(BinaryReader dataReader = new BinaryReader(dataStream)) {
          RawTextureheader rawHeader = dataReader.Read<RawTextureheader>();
          uint size = rawHeader.imageSize / header.Format().ByteSize();
          
          uint[] color1 = new uint[size];
          uint[] color2 = new uint[size];
          ushort[] color3 = new ushort[size];
          ushort[] color4 = new ushort[size];
          uint[] color5 = new uint[size];

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

          using(Stream ddsStream = File.Open(destFile, FileMode.OpenOrCreate, FileAccess.Write))
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
      }
    }
  }
}

using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct FF82DF73Header {
    public ulong unk1;
    public ulong unk2;
    public ulong count;
    public ulong offset;
  }

  public struct FF82DF73Mip {
    public ulong zero1;
    public STUDDataHeader f008_A; 
    public STUDDataHeader f008_B;
    public STUDDataHeader imageDefinition;
    public ulong zero2;  
  }

  public class FF82DF73 : STUDBlob {
    public static new uint id = 0xFF82DF73;
    public static new string name = "Decal";

    private FF82DF73Header header;
    private FF82DF73Mip[] mips;
    
    public FF82DF73Header Header => header;
    public FF82DF73Mip[] Mips => mips;

    public new void Dump(TextWriter writer) {
      writer.WriteLine("unk1: {0}", header.unk1);
      writer.WriteLine("unk2: {0}", header.unk1);
      writer.WriteLine("");
      writer.WriteLine("{0} definitions", header.count);
      for(ulong i = 0; i < header.count; ++i) {
        DumpKey(writer, mips[i].f008_A.key, "\t");
        DumpKey(writer, mips[i].f008_B.key, "\t");
        DumpKey(writer, mips[i].imageDefinition.key, "\t");
        writer.Write("");
      }
    }

    public new void Read(Stream input) {
      base.Read(input);
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<FF82DF73Header>();
        input.Position = (long)header.offset;
        mips = new FF82DF73Mip[header.count];
        for(ulong i = 0; i < header.count; ++i) {
          mips[i] = reader.Read<FF82DF73Mip>();
        }
      }
    }
  }
}

using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class Decal : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DecalHeader {
      public STUDInstanceInfo instance;
      public ulong unk0;
      public ulong unk1;
      public ulong textures;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DecalRecord {
      public OWRecord definiton;
    }

    public uint Id => 0xFF82DF73;
    public string Name => "Decal";

    private DecalHeader header;
    public DecalHeader Header => header;

    private DecalRecord[] records;
    public DecalRecord[] Records => records;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<DecalHeader>();

        input.Position = (long)header.textures;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        records = new DecalRecord[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          records[i] = reader.Read<DecalRecord>();
        }
      }
    }
  }
}

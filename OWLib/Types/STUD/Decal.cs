using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class Decal : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DecalHeader {
      public STUDInstanceInfo instance;
      public ulong offset;
      public ulong unk1;
      public ulong unk2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DecalRecord {
      public ulong unk1;
      public OWRecord f008A;
      public OWRecord f008B;
      public OWRecord definiton;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0x5DBB227A48364073;
      }
    }

    public string Name
    {
      get
      {
        return "Decal";
      }
    }

    private DecalHeader header;
    public DecalHeader Header => header;

    private DecalRecord[] records;
    public DecalRecord[] Records => records;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<DecalHeader>();

        input.Position = (long)header.offset;
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

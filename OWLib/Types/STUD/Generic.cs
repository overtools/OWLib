using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class GenericReference : ISTUDInstance {
    public uint Id => 0x8305C688;
    public string Name => "GenericReference";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ReferenceHeader {
      public STUDInstanceInfo instance;
      public ulong key;
    }

    private ReferenceHeader reference;
    public ReferenceHeader Reference => reference;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        reference = reader.Read<ReferenceHeader>();
      }
    }
  }

  public class GenericRecordReference : ISTUDInstance {
    public ulong Key => 0x9284FC578E1984E3;
    public uint Id => 0x528F2F94;
    public string Name => "GenericRecordReference";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ReferenceHeader {
      public STUDInstanceInfo instance;
      public OWRecord key;
    }

    private ReferenceHeader reference;
    public ReferenceHeader Reference => reference;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        reference = reader.Read<ReferenceHeader>();
      }
    }
  }
}

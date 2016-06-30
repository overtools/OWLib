using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class GenericReference : ISTUDInstance { // these are typically referenced directly from one of the other records.
    public ulong Key => 0xB87BF59ADE760C26;
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
}

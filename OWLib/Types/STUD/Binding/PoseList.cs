using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class PoseList : ISTUDInstance {
    public ulong Key => 0x6377FB6907E088FA;
    public uint Id => 0xDD3FC67D;
    public string Name => "Binding:PoseList";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct PoseListHeader {
      public STUDInstanceInfo instance;
      public OWRecord reference;
    }

    private PoseListHeader header;
    public PoseListHeader Header => header;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<PoseListHeader>();
      }
    }
  }
}

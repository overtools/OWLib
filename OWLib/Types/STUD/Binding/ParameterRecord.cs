using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class ParameterRecord : ISTUDInstance {
    public ulong Key => 0x11C983E815FECD28;
    public uint Id => 0x9B38211B;
    public string Name => "Binding:GameParameter";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ParameterHeader {
      public STUDInstanceInfo instance;
      public ulong arrayOffset;
      public ulong unk1;
      public uint unk2;
      public uint unk3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ParameterEntry {
      public OWRecord parameter;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
    }

    private ParameterHeader header;
    private ParameterEntry[] parameters;
    public ParameterHeader Header => header;
    public ParameterEntry[] Parameters => parameters;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<ParameterHeader>();
        if(header.arrayOffset == 0) {
          parameters = new ParameterEntry[0];
          return;
        }
        input.Position = (long)header.arrayOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        parameters = new ParameterEntry[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          parameters[i] = reader.Read<ParameterEntry>();
        }
      }
    }
  }
}

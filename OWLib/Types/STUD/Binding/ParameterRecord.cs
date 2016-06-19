using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class ParameterRecord : ISTUDInstance {
    public ulong Key
    {
      get
      {
        return 0x11C983E815FECD28;
      }
    }

    public string Name
    {
      get
      {
        return "Binding:GameParameter";
      }
    }

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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class ProjectileModelRecord : ISTUDInstance {
    public ulong Key => 0x9284FC578E1984E3;
    public uint Id => 0xEC23FFFD;
    public string Name => "Binding:ProjectileModel";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ProjectileModel {
      public STUDInstanceInfo instance;
      public OWRecord binding;
      public ulong offset;
      public ulong unk1;
      public ulong offsetInfo;
      public ulong unk2;
      public ulong unk3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BindingRecord {
      public ulong unk1;
      public OWRecord binding;
      public OWRecord virtualRecord;
      public OWRecord unk2;
      public ulong unk3;
    }

    private ProjectileModel header;
    private BindingRecord[] children;
    public ProjectileModel Header => header;
    public BindingRecord[] Children => children;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<ProjectileModel>();
        if(header.offset > 0) {
          input.Position = (long)header.offset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          children = new BindingRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            children[i] = reader.Read<BindingRecord>();
          }
        } else {
          children = new BindingRecord[0];
        }
      }
    }
  }
}

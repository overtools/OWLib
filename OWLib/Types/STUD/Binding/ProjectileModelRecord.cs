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
      public OWRecord unk;
      public OWRecord blank;
      public ulong offset;
      public ulong unk1;
      public ulong unk2;
    }

    private ProjectileModel header;
    public ProjectileModel Header => header;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<ProjectileModel>();
      }
    }
  }
}

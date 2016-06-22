using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class ChildParameterRecord : ISTUDInstance {
    public ulong Key
    {
      get
      {
        return 0x8D08A5795843FDD2;
      }
    }

    public string Name
    {
      get
      {
        return "Binding:ChildParameter";
      }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ChildParameter {
      public STUDInstanceInfo instance;
      public OWRecord binding;
      public ulong offset;
      public ulong offset2;
      public ulong offset3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Child {
      public OWRecord unk1;
      public ulong unk2;
      public ulong unk3;
      public ulong unk4;
      public OWRecord parameter;
    }

    private ChildParameter header;
    private Child[] children;
    public ChildParameter Header => header;
    public Child[] Children => children;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<ChildParameter>();
        if(header.offset > 0) {
          input.Position = (long)header.offset;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          children = new Child[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            children[i] = reader.Read<Child>();
          }
        } else {
          children = new Child[0];
        }
      }
    }
  }
}

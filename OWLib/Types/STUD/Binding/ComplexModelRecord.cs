using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
  public class ComplexModelRecord : ISTUDInstance {
    public ulong Key => 0xF65022A8B1E146D9;
    public uint Id => 0xBC1233E0;
    public string Name => "Binding:ComplexModel";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ComplexModel {
      public STUDInstanceInfo instance;
      public OWRecord animationList;
      public OWRecord funk1;
      public OWRecord model;
      public OWRecord material;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
      public float unk4;
      public uint unk5;
      public byte param1;
      public byte param2;
      public byte param3;
      public byte param4;
      public byte param5;
      public byte param6;
      public byte param7;
      public byte param8;
      public byte param9;
      public byte param10;
      public ushort unk6;
      public ushort unk7;
      public ushort unk8;
      public ushort unk9;
      public ushort unkA;
      public ushort unkB;
      public ushort unkC;
    }

    private ComplexModel data;
    public ComplexModel Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<ComplexModel>();
      }
    }
  }
}

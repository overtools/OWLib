using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
  public class BindingRecord : ISTUDInstance {
    public ulong Key => 0x0DECF2BA78E343C9;
    public uint Id => 0xE53C2921;
    public string Name => "GameParameter:Binding";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ModelParam {
      public STUDInstanceInfo instance;
      public ulong unk1;
      public OWRecord binding;
    }

    private ModelParam param;
    public ModelParam Param => param;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        param = reader.Read<ModelParam>();
      }
    }
  }
}

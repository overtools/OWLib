using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.GameParam {
  public class ChildGameParameterRecord : ISTUDInstance {
    public uint Id => 0x04E83493;
    public string Name => "GameParameter:Child";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ModelParam {
      public STUDInstanceInfo instance;
      public ulong unk1;
      public OWRecord binding;
      public OWRecord binding2;
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

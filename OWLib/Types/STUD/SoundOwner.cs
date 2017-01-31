using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class SoundOwner : ISTUDInstance {
    public uint Id => 0xCD24159B;
    public string Name => "Sound Owner";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SoundOwnerData {
      public STUDInstanceInfo instance;
      public OWRecord unk0;
      public OWRecord soundbank;
      public OWRecord unk2;
      public OWRecord unk3;
    }

    private SoundOwnerData data;
    public SoundOwnerData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<SoundOwnerData>();
      }
    }
  }
}
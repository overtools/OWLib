using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class SoundOwner : ISTUDInstance {
    public ulong Key => 0x9EB0A61CE3F03AEA;
    public uint Id => 0xCD24159B;
    public string Name => "Sound Owner";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SoundOwnerData {
      public STUDInstanceInfo instance;
      public OWRecord sound;
      public OWRecord unk1;
      public OWRecord soundbank;
      public OWRecord unk2;
      public OWRecord unk3;
      public ulong unk4;
      public ulong unk5;
      public float volume;
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
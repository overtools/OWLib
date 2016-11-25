using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class SoundMasterReference : ISTUDInstance {
    public uint Id => 0x8884C15A;
    public string Name => "Sound Master:Reference";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SoundMasterReferenceData {
      public STUDInstanceInfo instance;
      public OWRecord owner;
      public OWRecord sound;
      public OWRecord system;
    }

    private SoundMasterReferenceData data;
    public SoundMasterReferenceData Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<SoundMasterReferenceData>();
      }
    }
  }
}
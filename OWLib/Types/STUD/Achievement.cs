using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  [System.Diagnostics.DebuggerDisplay(OWLib.STUD.STUD_DEBUG_STR)]
  public class Achievement : ISTUDInstance {
    public uint Id => 0x7CE5C1B2;
    public string Name => "Achievement";
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AchievementData {
      public STUDInstanceInfo instance;
      public OWRecord name;
      public OWRecord description1;
      public OWRecord description2;
      public OWRecord image;
      public OWRecord reward;
      public OWRecord icon;
    }

    private AchievementData data;
    public AchievementData Data => data;

    public void Read(Stream input, OWLib.STUD stud) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<AchievementData>();
      }
    }
  }
}

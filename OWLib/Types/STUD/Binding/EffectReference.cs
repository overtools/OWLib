using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD.Binding {
    public class EffectReference : ISTUDInstance {
      public uint Id => 0x7454409A;

      public string Name => "Binding:EffectReference";
    
      [StructLayout(LayoutKind.Sequential, Pack = 4)]
      public struct ReferenceHeader {
          public STUDInstanceInfo instance;
          public OWRecord key;
      }

      private ReferenceHeader reference;
      public ReferenceHeader Reference => reference;

        public void Read(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                reference = reader.Read<ReferenceHeader>();
            }
        }
    }
}

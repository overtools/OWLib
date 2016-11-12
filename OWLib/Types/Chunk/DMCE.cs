using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
  class DMCE : IChunk {
    public string Identifier
    {
      get
      {
        return "DMCE"; // ECMD - Entity Child Model Data?
      }
    }

    public string RootIdentifier
    {
      get
      {
        return "TCFE"; // EFCT - Effect?
      }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Structure {
      public ulong modelKey;
      public ulong materialKey;
      public ulong animationKey;
      public ulong frame;
    }

    private Structure data;
    public Structure Data => data;

    public void Parse(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<Structure>();
      }
    }
  }
}

using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
  public class PRHM : IChunk {
    public string Identifier => "PRHM"; // MHRP - Model Hard Points
    public string RootIdentifier => "LDOM"; // MODL - Model

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Structure {
      public uint hardpointCount;
      public uint unkCount;
      public long hardpoint;
      public long unk;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct HardPoint {
      public Matrix4B matrix;
      public ulong HardPointGUID;
      public ulong GUIDx012;

      public ulong unk;
      public ulong unk2;
    }

    private Structure data;
    public Structure Data => data;

    private HardPoint[] hardpoints;
    public HardPoint[] HardPoints => hardpoints;

    public void Parse(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<Structure>();
        if(data.hardpointCount > 0 && data.hardpoint > 0) {
          hardpoints = new HardPoint[data.hardpointCount];
          input.Position = data.hardpoint;
          for(uint i = 0; i < data.hardpointCount; ++i) {
            hardpoints[i] = reader.Read<HardPoint>();
          }
        } else {
          hardpoints = new HardPoint[0];
        }
      }
    }
  }
}

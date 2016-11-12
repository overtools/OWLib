using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
  public class CLDM : IChunk {
    public string Identifier
    {
      get
      {
        return "CLDM"; // MDLC
      }
    }

    public string RootIdentifier
    {
      get
      {
        return "LDOM"; // MODL
      }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct Structure {
      public fixed float boundingBox[16];
      public byte unk1;
      public byte unk2;
      public ushort unk3;
      public uint unk4;
      public float unk5;
      public ushort materialCount;
      public ushort unk6;
      public long unk7;
      public long unk8;
      public long materialPointer;
    }

    private Structure data;
    public Structure Data => data;

    private ulong[] materials;
    public ulong[] Materials => materials;

    public void Parse(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<Structure>();
        if(data.materialCount > 0) {
          input.Position = data.materialPointer;
          materials = new ulong[data.materialCount];
          for(ushort i = 0; i < data.materialCount; ++i) {
            materials[i] = reader.ReadUInt64();
          }
        } else {
          materials = new ulong[0];
        }
      }
    }

    public ulong GetMaterial(ushort id) {
      if(materials == null || id >= materials.Length) {
        return 0;
      } else {
        return materials[id];
      }
    }
  }
}

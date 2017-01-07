using System.IO;
using System.Runtime.InteropServices;
using OpenTK;

namespace OWLib.Types.Chunk {
  public class lksm : IChunk {
    public string Identifier => "lksm"; // mskl - Model Skeleton
    public string RootIdentifier => "LDOM"; //  MODL - Model
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct Structure {
      public long hierarchy1;
      public long matrix44;
      public long matrix44i;
      public long matrix43;
      public long matrix43i;
      public long struct6;
      public long id;
      public long name;
      public long struct9;
      public long remap;
      public long hierarchy2;
      public int unk1;
      public ushort bonesAbs;
      public ushort bonesSimple;
      public ushort unk2;
      public ushort remapCount;
      public ushort idCount;
      public ushort unkStruct6Count;
      public ushort unk4;
      public ushort nameCount;
      public ushort unkStruct9Count;
      public ushort unk5;
      public ushort paddingSize;
    }

    private Structure data;
    public Structure Data => data;

    private Matrix4[] matrices;
    private Matrix4[] matricesInverted;
    private Matrix3x4[] matrices34;
    private Matrix3x4[] matrices34Inverted;
    private short[] hierarchy;
    private ushort[] lookup;
    private uint[] ids;

    public Matrix4[] Matrices => matrices;
    public Matrix4[] MatricesInverted => matricesInverted;
    public Matrix3x4[] Matrices34 => matrices34;
    public Matrix3x4[] Matrices34Inverted => matrices34Inverted;
    public short[] Hierarchy => hierarchy;
    public ushort[] Lookup => lookup;
    public uint[] IDs => ids;

    public void Parse(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        data = reader.Read<Structure>();

        hierarchy = new short[data.bonesAbs];
        input.Position = data.hierarchy1;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            input.Position += 4L;
            hierarchy[i] = reader.ReadInt16();
          }
        }

        matrices = new Matrix4[data.bonesAbs];
        matricesInverted = new Matrix4[data.bonesAbs];
        matrices34 = new Matrix3x4[data.bonesAbs];
        matrices34Inverted = new Matrix3x4[data.bonesAbs];

        input.Position = data.matrix44;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            matrices[i] = reader.Read<Matrix4B>().ToOpenTK();
          }
        }

        input.Position = data.matrix44i;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            matricesInverted[i] = reader.Read<Matrix4B>().ToOpenTK();
          }
        }

        input.Position = data.matrix43;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            matrices34[i] = reader.Read<Matrix3x4B>().ToOpenTK();
          }
        }

        input.Position = data.matrix43i;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            matrices34Inverted[i] = reader.Read<Matrix3x4B>().ToOpenTK();
          }
        }

        lookup = new ushort[data.remapCount];
        input.Position = data.remap;
        if(input.Position > 0) {
          for(int i = 0; i < data.remapCount; ++i) {
            lookup[i] = reader.ReadUInt16();
          }
        }

        ids = new uint[data.bonesAbs];
        input.Position = data.id;
        if(input.Position > 0) {
          for(int i = 0; i < data.bonesAbs; ++i) {
            ids[i] = reader.ReadUInt32();
          }
        }
      }
    }
  }
}

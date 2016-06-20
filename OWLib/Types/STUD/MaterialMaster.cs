using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class MaterialMaster : ISTUDInstance {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MaterialMasterHeader {
      public STUDInstanceInfo instance;
      public ulong materialOffset;
      public ulong zero1;
      public ulong unk1Offset;
      public ulong zero2;
      public ulong unk2Offset;
      public ulong zero3;
      public OWRecord unk3;
      public ulong bindOffset;
      public ulong zero4;
      public ulong dataOffset;
      public ulong zero5;
      public ulong zero6;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MaterialMasterMaterial {
      public OWRecord record;
      public ulong id;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MaterialMasterData {
      public ulong unk1;
      public OWRecord record;
      public ulong offset;
      public ulong unk2;
    }

    public ulong Key
    {
      get
      {
        return 0xABB8E85C7F65CBF9;
      }
    }

    public string Name
    {
      get
      {
        return "Material Master";
      }
    }

    private MaterialMasterHeader header;
    public MaterialMasterHeader Header => header;
    
    private MaterialMasterMaterial[] materials;
    private MaterialMasterMaterial[][] dataBinds;
    public MaterialMasterMaterial[] Materials => materials;
    public MaterialMasterMaterial[][] DataBinds => dataBinds;

    private OWRecord[] records1;
    private OWRecord[] binds;
    public OWRecord[] Records1 => records1;
    public OWRecord[] Binds => binds;
    
    private MaterialMasterData[] data;
    public MaterialMasterData[] Data => data;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<MaterialMasterHeader>();

        input.Position = (long)header.materialOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        materials = new MaterialMasterMaterial[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          materials[i] = reader.Read<MaterialMasterMaterial>();
        }

        if(header.unk1Offset > 0) {
          input.Position = (long)header.unk1Offset;
          ptr = reader.Read<STUDArrayInfo>();
          records1 = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            records1[i] = reader.Read<OWRecord>();
          }
        }

        if(header.bindOffset > 0) {
          input.Position = (long)header.bindOffset;
          ptr = reader.Read<STUDArrayInfo>();
          binds = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            binds[i] = reader.Read<OWRecord>();
          }
        }

        if(header.dataOffset > 0) {
          input.Position = (long)header.dataOffset;
          ptr = reader.Read<STUDArrayInfo>();
          data = new MaterialMasterData[ptr.count];
          dataBinds = new MaterialMasterMaterial[ptr.count][];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            data[i] = reader.Read<MaterialMasterData>();
          }
          for(ulong i = 0; i < ptr.count; ++i) {
            input.Position = (long)data[i].offset;
            STUDArrayInfo ptr2 = reader.Read<STUDArrayInfo>();
            dataBinds[i] = new MaterialMasterMaterial[ptr2.count];
            input.Position = (long)ptr2.offset;
            for(ulong j = 0; j < ptr2.count; ++j) {
              dataBinds[i][j] = reader.Read<MaterialMasterMaterial>();
            }
          }
        }
      }
    }
  }
}

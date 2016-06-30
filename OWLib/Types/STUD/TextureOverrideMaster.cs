using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class TextureOverrideMaster : ISTUDInstance {
    public ulong Key => 0x14E40F29D7CDF08F;
    public uint Id => 0xC25082A2;
    public string Name => "TextureOverrideMaster";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideMasterHeader {
      public STUDInstanceInfo instance;
      public OWRecord modelOverride;
      public uint unk1;
      public uint rarity;
      public ulong unk3;
      public ulong zero1;
      public ulong zero2;
      public ulong offsetInfo;
      public ulong zero4;
      public OWRecord name;
      public ulong zero5;
      public ulong zero6;
      public ulong zero7;
      public ulong zero8;
      public ulong offsetRecords;
      public ulong zero9;
      public OWRecord icon;
      public ulong zeroA;
      public ulong zeroB;
      public ulong zeroC;
      public ulong zeroD;
      public ulong unk4;
    }

    private TextureOverrideMasterHeader header;
    private TextureOverride.TextureOverrideInlineReference[] references;
    private ulong[] replace;
    private ulong[] target;
    private uint[] sizes;
    private OWRecord[] records;

    public TextureOverrideMasterHeader Header => header;
    public OWRecord[] Records => records;
    public TextureOverride.TextureOverrideInlineReference[] References => references;
    public ulong[] Replace => replace;
    public ulong[] Target => target;
    public uint[] Sizes => sizes;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<TextureOverrideMasterHeader>();
        if(header.offsetRecords > 0) {
          input.Position = (long)header.offsetRecords;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          records = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            records[i] = reader.Read<OWRecord>();
          }
        }

        if(header.offsetInfo == 0) {
          return;
        }

        input.Position = (long)header.offsetInfo;
        STUDReferenceArrayInfo info = reader.Read<STUDReferenceArrayInfo>();

        uint size = 0;
        sizes = new uint[info.count];
        input.Position = (long)info.indiceOffset;
        for(ulong i = 0; i < info.count; ++i) {
          sizes[i] = reader.ReadUInt32();
          if(sizes[i] > size) {
            size = sizes[i];
          }
        }

        replace = new ulong[size];
        references = new TextureOverride.TextureOverrideInlineReference[size];
        input.Position = (long)info.referenceOffset;
        for(uint i = 0; i < size; ++i) {
          references[i] = reader.Read<TextureOverride.TextureOverrideInlineReference>();
          replace[i] = references[i].key;
        }
        target = new ulong[size];
        for(uint i = 0; i < size; ++i) {
          input.Position = (long)(references[i].offset + 8UL);
          target[i] = reader.ReadUInt64();
        }
      }
    }
  }
}

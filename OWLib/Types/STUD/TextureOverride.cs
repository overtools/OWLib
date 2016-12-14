using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class TextureOverride : ISTUDInstance {
    public uint Id => 0x4FB2CE32;
    public string Name => "TextureOverride";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideHeader {
      public STUDInstanceInfo instance;
      public OWRecord zero1;
      public uint rarity;
      public uint unk1;
      public ulong one;
      public ulong unk2;
      public ulong unk3;
      public ulong unk1600_1;
      public ulong unk1600_2;
      public ulong offsetInfo;
      public ulong unk5;
      public ulong unk6;
      public ulong unk7;
      public OWRecord zero2;
      public ulong offset0AD;
      public ulong unk8;
      public ulong unk9;
      public ulong unkA;
      public OWRecord unk1600_3;
      public Vec4 delta;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideInlineReference {
      public ulong key;
      public ulong offset;
    }
    
    private TextureOverrideHeader header;
    private TextureOverrideInlineReference[] references;
    private ulong[] replace;
    private ulong[] target;
    private uint[] sizes;
    private OWRecord[] subs;

    public TextureOverrideHeader Header => header;
    public TextureOverrideInlineReference[] References => references;
    public ulong[] Replace => replace;
    public ulong[] Target => target;
    public uint[] Sizes => sizes;
    public OWRecord[] SubDefinitions => subs;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<TextureOverrideHeader>();
        if(header.offsetInfo > 0) {
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
          references = new TextureOverrideInlineReference[size];
          input.Position = (long)info.referenceOffset;
          for(uint i = 0; i < size; ++i) {
            references[i] = reader.Read<TextureOverrideInlineReference>();
            replace[i] = references[i].key;
          }
          target = new ulong[size];
          for(uint i = 0; i < size; ++i) {
            input.Position = (long)(references[i].offset + 8UL);
            target[i] = reader.ReadUInt64();
          }
        } else {
          target = new ulong[0];
          references = new TextureOverrideInlineReference[0];
          replace = new ulong[0];
          sizes = new uint[0];
        }

        if(header.offset0AD > 0) {
          input.Position = (long)header.offset0AD;
          STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
          subs = new OWRecord[ptr.count];
          input.Position = (long)ptr.offset;
          for(ulong i = 0; i < ptr.count; ++i) {
            subs[i] = reader.Read<OWRecord>();
          }
        } else {
          subs = new OWRecord[0];
        }
      }
    }
  }

  public class TextureOverrideSecondary : ISTUDInstance {
    public ulong Key => 0x7B9E6EF05C0F92F8;
    public uint Id => 0x3FCD3E39;
    public string Name => "TextureOverride:Secondary";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideSecondaryHeader {
      public STUDInstanceInfo instance;
      public OWRecord zero1;
      public ulong unk1;
      public ulong one;
      public ulong unk2;
      public ulong unk3;
      public ulong unk1600_1;
      public ulong unk1600_2;
      public ulong offsetInfo;
      public ulong unk5;
      public ulong unk6;
      public ulong unk7;
      public ulong unk8;
    }

    private TextureOverrideSecondaryHeader header;
    private TextureOverride.TextureOverrideInlineReference[] references;
    private ulong[] replace;
    private ulong[] target;
    private uint[] sizes;

    public TextureOverrideSecondaryHeader Header => header;
    public TextureOverride.TextureOverrideInlineReference[] References => references;
    public ulong[] Replace => replace;
    public ulong[] Target => target;
    public uint[] Sizes => sizes;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<TextureOverrideSecondaryHeader>();
        if(header.offsetInfo > 0) {
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
        } else {
          target = new ulong[0];
          references = new TextureOverride.TextureOverrideInlineReference[0];
          replace = new ulong[0];
          sizes = new uint[0];
        }
      }
    }
  }
}

using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class TextureOverride : ISTUDInstance {
    public ulong Key => 0x053C4CCBC528B5B3;
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
      public ulong offsetInfo;
      public ulong unk5;
      public OWRecord zero2;
      public ulong offset0AD;
      public ulong unk6;
      public ulong unk7;
      public ulong unk8;
      public OWRecord f058;
      public Vec4 delta;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideInfo {
      public ulong count;
      public ulong indiceOffset;
      public ulong replaceOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideInlineReference {
      public ulong key;
      public ulong offset;
    }
    
    private TextureOverrideHeader header;
    private TextureOverrideInfo info;
    private TextureOverrideInlineReference[] references;
    private ulong[] replace;
    private ulong[] target;
    private uint[] sizes;
    private OWRecord[] subs;

    public TextureOverrideHeader Header => header;
    public TextureOverrideInfo Info => info;
    public TextureOverrideInlineReference[] References => references;
    public ulong[] Replace => replace;
    public ulong[] Target => target;
    public uint[] Sizes => sizes;
    public OWRecord[] SubDefinitions => subs;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<TextureOverrideHeader>();
        input.Position = (long)header.offsetInfo;
        info = reader.Read<TextureOverrideInfo>();

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
        input.Position = (long)info.replaceOffset;
        for(uint i = 0; i < size; ++i) {
          references[i] = reader.Read<TextureOverrideInlineReference>();
          replace[i] = references[i].key;
        }
        target = new ulong[size];
        for(uint i = 0; i < size; ++i) {
          input.Position = (long)(references[i].offset + 8UL);
          target[i] = reader.ReadUInt64();
        }

        input.Position = (long)header.offset0AD;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();
        subs = new OWRecord[ptr.count];
        input.Position = (long)ptr.offset;
        for(ulong i = 0; i < ptr.count; ++i) {
          subs[i] = reader.Read<OWRecord>();
        }
      }
    }
  }

  public class TextureOverrideSecondary : ISTUDInstance {
    public ulong Key => 0x7B9E6EF05C0F92F8;
    public string Name => "TextureOverride:Secondary";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureOverrideSecondaryHeader {
      public STUDInstanceInfo instance;
      public OWRecord zero1;
      public ulong unk1;
      public ulong one;
      public ulong unk2;
      public ulong unk3;
      public ulong offsetInfo;
      public ulong unk5;
      public ulong unk6;
      public ulong unk7;
      public ulong unk8;
    }

    private TextureOverrideSecondaryHeader header;
    private TextureOverride.TextureOverrideInfo info;
    private TextureOverride.TextureOverrideInlineReference[] references;
    private ulong[] replace;
    private ulong[] target;
    private uint[] sizes;

    public TextureOverrideSecondaryHeader Header => header;
    public TextureOverride.TextureOverrideInfo Info => info;
    public TextureOverride.TextureOverrideInlineReference[] References => references;
    public ulong[] Replace => replace;
    public ulong[] Target => target;
    public uint[] Sizes => sizes;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<TextureOverrideSecondaryHeader>();
        input.Position = (long)header.offsetInfo;
        info = reader.Read<TextureOverride.TextureOverrideInfo>();

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
        input.Position = (long)info.replaceOffset;
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

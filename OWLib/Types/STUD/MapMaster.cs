using System;
using System.IO;
using System.Runtime.InteropServices;


namespace OWLib.Types.STUD {
  public class MapMaster : ISTUDInstance {
    public ulong Key => 0xC670329155D47265;

    public string Name => "Map Master";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MapMasterHeader {
      public STUDInstanceInfo instance;
      public OWRecord f002;
      public OWRecord data;
      public OWRecord background;
      public OWRecord flag;
      public OWRecord f00D1;
      public OWRecord f00D2;
      public ulong unk1;
      public ulong unk2;
      public ulong unk3;
      public ulong unk4;
      public ulong unk5;
      public ulong unk6;
      public OWRecord name;
      public OWRecord subline;
      public OWRecord stateA;
      public OWRecord stateB;
      public OWRecord description1;
      public OWRecord description2;
      public OWRecord texture1;
      public OWRecord unkRecord1;
      public OWRecord unkRecord2;
      public uint unk7;
      public uint unk8;
      public uint unk9;
      public uint unkA;
    }

    private MapMasterHeader header;
    public MapMasterHeader Header => header;

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<MapMasterHeader>();

        header.data.key = (header.data.key & ~0xFFFFFFFF00000000ul) | 0x0DD0000100000000ul;
      }
    }
  }
}

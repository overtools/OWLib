using System;
using System.Runtime.InteropServices;
using System.IO;

namespace OWLib.Types.STUD.InventoryItem {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct InventoryItemHeader {
    public STUDInstanceInfo instance;
    public OWRecord name;
    public OWRecord icon;
    public OWRecord unk1;
    public uint rarity;
    public uint amount;
  };
}

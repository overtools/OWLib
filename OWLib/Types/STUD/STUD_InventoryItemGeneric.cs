using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OWLib.Types.STUD {
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
  public struct STUDInventoryItemHeader {
    public ulong stringKey;
    public ulong padding1;
    public ulong textureKey;
    public ulong padding2;
    public ulong unk1;
    public uint rarity;
    public uint amount;
  }

  public class STUDInventoryItemGeneric : STUDBlob {
    public static new string name = "Inventory STUD";

    private STUDInventoryItemHeader inventoryHeader;
    public STUDInventoryItemHeader InventoryHeader => inventoryHeader;

    public void DumpInventoryItemHeader(TextWriter writer, STUDInventoryItemHeader header, string padding = "") {
      writer.WriteLine("{0}string:", padding);
      DumpKey(writer, header.stringKey, padding + "\t");
      writer.WriteLine("{0}texture:", padding);
      DumpKey(writer, header.textureKey, padding + "\t");
      writer.WriteLine("{0}unk1: {1}", padding, header.unk1);
      writer.WriteLine("{0}rarity: {1}", padding, header.rarity);
      writer.WriteLine("{0}amount: {1}", padding, header.amount);
    }

    public new void Dump(TextWriter writer) {
      writer.WriteLine("inventory:");
      DumpInventoryItemHeader(writer, inventoryHeader, "\t");
    }

    public new void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        inventoryHeader = reader.Read<STUDInventoryItemHeader>();
      }
    }
  }
}

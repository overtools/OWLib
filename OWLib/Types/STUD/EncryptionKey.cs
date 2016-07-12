using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.STUD {
  public class EncryptionKey : ISTUDInstance {
    public uint Id => 0x8F754DFF;

    public ulong Key => 0xE68A7714016119F7;

    public string Name => "Encryption Key";

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EncryptionKeyHeader {
      public STUDInstanceInfo instance;
      public ulong zero1;
      public ulong unk1;
      public ulong unk2;
      public ulong offsetName;
      public ulong zero2;
      public ulong offsetKey;
      public ulong zero3;
      public uint zero4;
      public ulong unk3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct EncryptionKeySlice {
      public uint size;
      public uint checksum;
      public ulong offset;
    }

    private EncryptionKeyHeader header;
    private EncryptionKeySlice nameSlice;
    private EncryptionKeySlice keySlice;
    private byte[] keyName;
    private byte[] keyValue;

    public EncryptionKeyHeader Header => header;
    public EncryptionKeySlice NameSlice => nameSlice;
    public EncryptionKeySlice KeySlice => keySlice;
    public byte[] KeyName => keyName;
    public string KeyNameText {
      get {
        string x = "";
        for(int i = KeyName.Length - 1; i > 0; i -= 2) {
          char h = (char)KeyName[i];
          char l = (char)KeyName[i-1];
          x += l.ToString() + h.ToString();
        }
        return x.ToUpperInvariant();
      }
    }
    public byte[] KeyValue => keyValue;
    public string KeyValueText {
      get {
        return BitConverter.ToString(KeyValue).Replace("-", string.Empty);
      }
    }

    public void Read(Stream input) {
      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
        header = reader.Read<EncryptionKeyHeader>();

        input.Position = (long)header.offsetName;
        nameSlice = reader.Read<EncryptionKeySlice>();
        input.Position = (long)header.offsetKey;
        keySlice = reader.Read<EncryptionKeySlice>();
        
        input.Position = (long)nameSlice.offset;
        keyName = reader.ReadBytes((int)nameSlice.size);
        input.Position = (long)keySlice.offset;
        keyValue = reader.ReadBytes((int)keySlice.size);
      }
    }
  }
}

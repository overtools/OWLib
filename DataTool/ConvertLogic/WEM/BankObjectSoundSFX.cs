using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(2)]
    public class BankObjectSoundSFX : IBankObject {
        public enum SoundLocation : byte {
            Embedded = 0,
            Streamed = 1,
            StreamedZeroLatency = 2
        }

        public uint SoundID;
        public SoundLocation Location;

        public void Read(BinaryReader reader) {
            // using a different structure to the wiki :thinking:
            Location = (SoundLocation) reader.ReadByte();

            ushort u1 = reader.ReadUInt16();
            ushort u2 = reader.ReadUInt16();

            SoundID = reader.ReadUInt32();
        }
    }
}
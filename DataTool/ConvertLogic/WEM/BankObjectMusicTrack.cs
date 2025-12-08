using System.Collections.Generic;
using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(11)]
    public class BankObjectMusicTrack : IBankObject {
        public List<BankSourceData> Sources = [];

        public void Read(BinaryReader reader) {
            var flags = reader.ReadByte();
            var numSources = reader.ReadUInt32();

            Sources.EnsureCapacity(checked((int)numSources));
            for (int i = 0; i < numSources; i++) {
                Sources.Add(new BankSourceData(reader));
            }
        }
    }

    public class BankSourceData {
        public uint PluginID;
        public byte StreamType;
        public BankMediaInformation Media;

        public BankSourceData(BinaryReader reader) {
            PluginID = reader.ReadUInt32();
            StreamType = reader.ReadByte();
            Media = new BankMediaInformation(reader);

            if ((PluginID & 0xF) == 2) { // source
                reader.ReadUInt32();
            }
        }
    }

    public class BankMediaInformation {
        public uint SourceID;
        public uint InMemoryMediaSize;
        public byte SourceBits;

        public BankMediaInformation(BinaryReader reader) {
            SourceID = reader.ReadUInt32();
            InMemoryMediaSize = reader.ReadUInt32();
            SourceBits = reader.ReadByte();
        }
    }
}
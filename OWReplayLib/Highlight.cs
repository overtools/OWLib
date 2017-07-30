using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using static OWReplayLib.Types.Highlight;
using OverTool;

namespace OWReplayLib {
    public class Highlight : IDisposable {
        private CASCHandler handler = null;
        private OwRootHandler root = null;

        private HighlightHeader header;
        private Replay embeddedReplay;

        public HighlightHeader Header => header;
        public Replay EmbeddedReplay => embeddedReplay;

        private Dictionary<ulong, Record> records = new Dictionary<ulong, Record>();

        public Highlight(Stream stream, CASCHandler handler, Dictionary<ulong, Record> records = null) {
            if (records != null) {
                this.records = records;
            }

            this.handler = handler;
            root = handler?.Root as OwRootHandler;
            Util.MapCMF(root, handler, records, null, null);

            using (BinaryReader reader = new BinaryReader(stream)) {
                header = reader.Read<HighlightHeader>();
                if (header.Magic != MAGIC_CONSTANT) {
                    throw new InvalidDataException("Data stream is not a highlight!");
                }
                MemoryStream replayData = new MemoryStream(header.ReplayLength);
                stream.CopyBytes(replayData, header.ReplayLength);
                embeddedReplay = new Replay(replayData, handler, records);
            }
        }

        public void Dispose() {
            embeddedReplay?.Dispose();
        }
    }
}

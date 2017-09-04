using System;
using System.Collections.Generic;
using System.IO;
using CASCLib;
using OverTool;
using static OWReplayLib.Types.Replay;
using ZstdNet;

namespace OWReplayLib {
    public class Replay : IDisposable {
        private CASCHandler handler;
        private OwRootHandler root;
        private Dictionary<ulong, Record> records;
        private MemoryStream decompressedStream;

        private ReplayHeader header;
        private SortedList<double, ReplayFrame> frames = new SortedList<double, ReplayFrame>();

        public ReplayHeader Header => header;
        public SortedList<double, ReplayFrame> Frames => frames;

        public Replay(Stream stream, CASCHandler handler, Dictionary<ulong, Record> records = null) {
            if (records != null) {
                this.records = records;
            }

            this.handler = handler;
            root = handler?.Root as OwRootHandler;
            Util.MapCMF(root, handler, records, null, null);

            using (BinaryReader reader = new BinaryReader(stream)) {
                header = reader.Read<ReplayHeader>();
                if (header.Magic != MAGIC_CONSTANT) {
                    throw new InvalidDataException("Data stream is not a replay!");
                }
                using (Decompressor decompressor = new Decompressor()) {
                    byte[] temp = reader.ReadBytes((int)(stream.Length - stream.Position));
                    byte[] dec = decompressor.Unwrap(temp);
                    decompressedStream = new MemoryStream(dec);
                }
            }
        }

        public void Dispose() {
            decompressedStream?.Dispose();
            foreach(ReplayFrame frame in Frames.Values) {
                frame?.Dispose();
            }
        }
    }
}

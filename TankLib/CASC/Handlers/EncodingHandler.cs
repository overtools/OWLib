using System.Collections.Generic;
using System.IO;
using TankLib.CASC.Helpers;

namespace TankLib.CASC.Handlers {
    public struct EncodingEntry {
        public MD5Hash Key;
        public int Size;
    }

    public class EncodingHandler {
        private static readonly MD5HashComparer Comparer = new MD5HashComparer();
        private Dictionary<MD5Hash, EncodingEntry> _encodingData = new Dictionary<MD5Hash, EncodingEntry>(Comparer);
        private const int ChunkSize = 4096;
        public int Count => _encodingData.Count;

        public EncodingHandler(BinaryReader stream, ProgressReportSlave worker) {
            worker?.ReportProgress(0, "Loading \"encoding\"...");

            stream.Skip(2); // EN
            byte b1 = stream.ReadByte();
            byte checksumSizeA = stream.ReadByte();
            byte checksumSizeB = stream.ReadByte();
            ushort flagsA = stream.ReadUInt16();
            ushort flagsB = stream.ReadUInt16();
            int numEntriesA = stream.ReadInt32BE();
            int numEntriesB = stream.ReadInt32BE();
            byte b4 = stream.ReadByte();
            int stringBlockSize = stream.ReadInt32BE();

            stream.Skip(stringBlockSize);
            //string[] strings = Encoding.ASCII.GetString(stream.ReadBytes(stringBlockSize)).Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            stream.Skip(numEntriesA * 32);
            //for (int i = 0; i < numEntriesA; ++i)
            //{
            //    byte[] firstHash = stream.ReadBytes(16);
            //    byte[] blockHash = stream.ReadBytes(16);
            //}

            long chunkStart = stream.BaseStream.Position;

            for (int i = 0; i < numEntriesA; ++i) {
                ushort keysCount;

                while ((keysCount = stream.ReadUInt16()) != 0) {
                    int fileSize = stream.ReadInt32BE();
                    MD5Hash md5 = stream.Read<MD5Hash>();

                    EncodingEntry entry = new EncodingEntry {
                        Size = fileSize
                    };

                    // how do we handle multiple keys?
                    for (int ki = 0; ki < keysCount; ++ki) {
                        MD5Hash key = stream.Read<MD5Hash>();
                        
                        // use first key for now
                        if (ki == 0)
                            entry.Key = key;
                        else {
                            // todo: log spam
                            //Debugger.Log(0, "CASC", $"Multiple encoding keys for MD5 {md5.ToHexString()}: {key.ToHexString()}\r\n");
                        }
                    }
                    
                    _encodingData.Add(md5, entry);
                }

                // each chunk is 4096 bytes, and zero padding at the end
                long remaining = ChunkSize - (stream.BaseStream.Position - chunkStart) % ChunkSize;

                if (remaining > 0)
                    stream.BaseStream.Position += remaining;

                worker?.ReportProgress((int) ((i + 1) / (float) numEntriesA * 100));
            }

            stream.Skip(numEntriesB * 32);

            long chunkStart2 = stream.BaseStream.Position;

            for (int i = 0; i < numEntriesB; ++i) {
                byte[] key = stream.ReadBytes(16);
                int stringIndex = stream.ReadInt32BE();
                byte unk1 = stream.ReadByte();
                int fileSize = stream.ReadInt32BE();

                // each chunk is 4096 bytes, and zero padding at the end
                long remaining = ChunkSize - (stream.BaseStream.Position - chunkStart2) % ChunkSize;

                if (remaining > 0)
                    stream.BaseStream.Position += remaining;
            }

            // string block till the end of file
        }

        public IEnumerable<KeyValuePair<MD5Hash, EncodingEntry>> Entries {
            get {
                foreach (KeyValuePair<MD5Hash, EncodingEntry> entry in _encodingData)
                    yield return entry;
            }
        }

        public bool GetEntry(MD5Hash md5, out EncodingEntry enc)
        {
            return _encodingData.TryGetValue(md5, out enc);
        }

        public bool HasEntry(MD5Hash md5)
        {
            return _encodingData.ContainsKey(md5);
        }

        public void Clear() {
            _encodingData.Clear();
            _encodingData = null;
        }
    }
}
/*namespace TankLib.CASC.Handlers {
    public class DownloadEntry {
        public int Index;

        public IEnumerable<KeyValuePair<string, DownloadTag>> Tags;
    }

    public class DownloadTag {
        public short Type;
        public BitArray Bits;
    }
    
    public class DownloadHandler {
        private static readonly MD5HashComparer Comparer = new MD5HashComparer();
        private Dictionary<MD5Hash, DownloadEntry> _downloadData = new Dictionary<MD5Hash, DownloadEntry>(Comparer);
        private Dictionary<string, DownloadTag> _tags = new Dictionary<string, DownloadTag>();

        public int Count => _downloadData.Count;

        public DownloadHandler(BinaryReader stream, BackgroundWorkerEx worker) {
            worker?.ReportProgress(0, "Loading \"download\"...");

            stream.Skip(2); // DL

            byte b1 = stream.ReadByte();
            byte b2 = stream.ReadByte();
            byte b3 = stream.ReadByte();

            int numFiles = stream.ReadInt32BE();

            short numTags = stream.ReadInt16BE();

            int numMaskBytes = (numFiles + 7) / 8;

            for (int i = 0; i < numFiles; i++)
            {
                MD5Hash key = stream.Read<MD5Hash>();

                //byte[] unk = stream.ReadBytes(0xA);
                stream.Skip(0xA);

                //var entry = new DownloadEntry() { Index = i, Unk = unk };
                var entry = new DownloadEntry() { Index = i };

                _downloadData.Add(key, entry);

                worker?.ReportProgress((int)((i + 1) / (float)numFiles * 100));
            }

            for (int i = 0; i < numTags; i++) {
                DownloadTag tag = new DownloadTag();
                string name = stream.ReadCString();
                tag.Type = stream.ReadInt16BE();

                byte[] bits = stream.ReadBytes(numMaskBytes);

                for (int j = 0; j < numMaskBytes; j++)
                    bits[j] = (byte)((bits[j] * 0x0202020202 & 0x010884422010) % 1023);

                tag.Bits = new BitArray(bits);

                _tags.Add(name, tag);
            }
        }

        public void Dump() {
            foreach (var entry in _downloadData) {
                if (entry.Value.Tags == null)
                    entry.Value.Tags = _tags.Where(kv => kv.Value.Bits[entry.Value.Index]);

                Logger.WriteLine("{0} {1}", entry.Key.ToHexString(), string.Join(",", entry.Value.Tags.Select(tag => tag.Key)));
            }
        }

        public DownloadEntry GetEntry(MD5Hash key) {
            _downloadData.TryGetValue(key, out DownloadEntry entry);

            if (entry != null && entry.Tags == null)
                entry.Tags = _tags.Where(kv => kv.Value.Bits[entry.Index]);

            return entry;
        }

        public void Clear() {
            _tags.Clear();
            _tags = null;
            _downloadData.Clear();
            _downloadData = null;
        }
    }
}*/
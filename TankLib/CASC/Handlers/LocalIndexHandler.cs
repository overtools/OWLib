using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TankLib.CASC.Helpers;

namespace TankLib.CASC.Handlers {
    public class LocalIndexHandler {
        private static readonly MD5HashComparer Comparer = new MD5HashComparer();
        private Dictionary<MD5Hash, IndexEntry> _localIndexData = new Dictionary<MD5Hash, IndexEntry>(Comparer);

        public int Count => _localIndexData.Count;

        private LocalIndexHandler() {}

        public static LocalIndexHandler Initialize(CASCConfig config, BackgroundWorkerEx worker) {
            LocalIndexHandler handler = new LocalIndexHandler();

            List<string> idxFiles = GetIdxFiles(config);

            if (idxFiles.Count == 0)
                throw new FileNotFoundException("idx files missing!");

            worker?.ReportProgress(0, "Loading \"local indexes\"...");

            int idxIndex = 0;

            foreach (string idx in idxFiles) {
                handler.ParseIndex(idx);

                worker?.ReportProgress((int)(++idxIndex / (float)idxFiles.Count * 100));
            }

            Debugger.Log(0, "CASC", $"LocalIndexHandler: loaded {handler.Count} indexes\r\n");

            return handler;
        }

        private unsafe void ParseIndex(string idx) {
            using (FileStream fs = new FileStream(idx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(fs)) {
                int h2Len = br.ReadInt32();
                int h2Check = br.ReadInt32();
                byte[] h2 = br.ReadBytes(h2Len);

                long padPos = (8 + h2Len + 0x0F) & 0xFFFFFFF0;
                fs.Position = padPos;

                int dataLen = br.ReadInt32();
                int dataCheck = br.ReadInt32();

                int numBlocks = dataLen / 18;

                //byte[] buf = new byte[8];

                for (int i = 0; i < numBlocks; i++) {
                    IndexEntry info = new IndexEntry();
                    byte[] keyBytes = br.ReadBytes(9);
                    Array.Resize(ref keyBytes, 16);

                    MD5Hash key;

                    fixed (byte *ptr = keyBytes)
                        key = *(MD5Hash*)ptr;

                    byte indexHigh = br.ReadByte();
                    int indexLow = br.ReadInt32BE();

                    info.Index = indexHigh << 2 | (byte)((indexLow & 0xC0000000) >> 30);
                    info.Offset = indexLow & 0x3FFFFFFF;

                    //for (int j = 3; j < 8; j++)
                    //    buf[7 - j] = br.ReadByte();

                    //long val = BitConverter.ToInt64(buf, 0);
                    //info.Index = (int)(val / 0x40000000);
                    //info.Offset = (int)(val % 0x40000000);

                    info.Size = br.ReadInt32();

                    // duplicate keys wtf...
                    //IndexData[key] = info; // use last key
                    if (!_localIndexData.ContainsKey(key)) // use first key
                        _localIndexData.Add(key, info);
                }

                padPos = (dataLen + 0x0FFF) & 0xFFFFF000;
                fs.Position = padPos;

                fs.Position += numBlocks * 18;
                //for (int i = 0; i < numBlocks; i++)
                //{
                //    var bytes = br.ReadBytes(18); // unknown data
                //}

                //if (fs.Position != fs.Length)
                //    throw new Exception("idx file under read");
            }
        }

        private static List<string> GetIdxFiles(CASCConfig config) {
            List<string> latestIdx = new List<string>();

            string dataFolder = CASCConfig.GetDataFolder();
            string dataPath = Path.Combine(dataFolder, "data");

            for (int i = 0; i < 0x10; ++i) {
                List<string> files = Directory.EnumerateFiles(Path.Combine(config.BasePath, dataPath), $"{i:X2}*.idx").ToList();

                if (files.Any())
                    latestIdx.Add(files.Last());
            }

            return latestIdx;
        }

        public unsafe IndexEntry GetIndexInfo(MD5Hash key) {
            // todo: wot does this do?
            //ulong* ptr = (ulong*)&key;
            //ptr[1] &= 0xFF;

            if (!_localIndexData.TryGetValue(key, out IndexEntry result))
                Debugger.Log(0, "CASC", $"LocalIndexHandler: missing index: {key.ToHexString()}\r\n");

            return result;
        }

        public void Clear() {
            _localIndexData.Clear();
            _localIndexData = null;
        }
    }
}
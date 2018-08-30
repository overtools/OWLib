using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TankLib.CASC.Helpers;

namespace TankLib.CASC.Handlers {
    public class LocalIndexHandler {
        private static readonly MD5HashComparer Comparer = new MD5HashComparer();
        private readonly CASCConfig _config;
        
        /// <summary>Local indices</summary>
        public Dictionary<MD5Hash, IndexEntry> Indices = new Dictionary<MD5Hash, IndexEntry>(Comparer);
        
        /// <summary>Number of local indices</summary>
        public int Count => Indices.Count;

        /// <summary>Internal file streams</summary>
        private readonly Dictionary<int, Stream> _dataStreams;
        
        /// <summary>
        /// Data stream locks. Prevents multiple threads from using the stream at the same time 
        /// </summary>
        private readonly Dictionary<int, object[]> _dataLocks;


        private LocalIndexHandler(CASCConfig config) {
            _config = config;
            
            _dataLocks = new Dictionary<int, object[]>();
            _dataStreams = new Dictionary<int, Stream>();

            for (int i = 0; i < 0x40; i++) { // err, hopefully enough
                _dataLocks[i] = new object[0];
            }
        }

        public static LocalIndexHandler Initialize(CASCConfig config, ProgressReportSlave worker) {
            LocalIndexHandler handler = new LocalIndexHandler(config);

            List<string> idxFiles = GetIdxFiles(config);

            if (idxFiles.Count == 0)
                throw new FileNotFoundException("idx files missing!");

            worker?.ReportProgress(0, "Loading \"local indexes\"...");

            int idxIndex = 0;

            foreach (string idx in idxFiles) {
                handler.ParseIndex(idx);

                worker?.ReportProgress((int)(++idxIndex / (float)idxFiles.Count * 100));
            }

            return handler;
        }

        private unsafe void ParseIndex(string idx) {
            using (FileStream fs = File.OpenRead(idx))
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
                    if (!Indices.ContainsKey(key)) // use first key
                        Indices.Add(key, info);
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
            ulong* ptr = (ulong*)&key;
            ptr[1] &= 0xFF;

            if (!Indices.TryGetValue(key, out IndexEntry result)) {
                Debugger.Log(0, "CASC", $"LocalIndexHandler: missing index: {key.ToHexString()}\r\n");
            }

            return result;
        }

        public Stream OpenIndexInfo(IndexEntry idxInfo, MD5Hash key, bool checkHash = true) {
            lock (_dataLocks[idxInfo.Index]) {
                Stream dataStream = GetDataStream(idxInfo.Index);
                dataStream.Position = idxInfo.Offset;

                using (BinaryReader reader = new BinaryReader(dataStream, Encoding.ASCII, true)) {
                    byte[] md5 = reader.ReadBytes(16);
                    Array.Reverse(md5);

                    if (checkHash) {
                        if (!key.EqualsTo9(md5))
                            throw new Exception("local data corrupted");
                    }

                    int size = reader.ReadInt32();

                    if (size != idxInfo.Size)
                        throw new Exception("local data corrupted");

                    //byte[] unkData1 = reader.ReadBytes(2);
                    //byte[] unkData2 = reader.ReadBytes(8);
                    dataStream.Position += 10;

                    byte[] data = reader.ReadBytes(idxInfo.Size - 30);

                    return new MemoryStream(data);
                }
            }
        }

        private Stream GetDataStream(int index) {
            if (_dataStreams.TryGetValue(index, out Stream stream))
                return stream;

            string dataFolder = CASCConfig.GetDataFolder();
            string dataFile = Path.Combine(_config.BasePath, dataFolder, "data", $"data.{index:D3}");

            stream = File.OpenRead(dataFile);

            _dataStreams[index] = stream;

            return stream;
        }


        public void Clear() {
            Indices.Clear();
            Indices = null;
        }
    }
}
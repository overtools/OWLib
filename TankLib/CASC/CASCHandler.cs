using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    /// <summary>
    /// Content Addressable Storage Container (CASC) handler
    /// </summary>
    public class CASCHandler {
        /// <summary>Local indices</summary>
        private readonly LocalIndexHandler _localIndex;
        
        /// <summary>CDN (remote) indices</summary>
        private readonly CDNIndexHandler _cdnIndex;
        
        /// <summary>Internal file streams</summary>
        private readonly Dictionary<int, Stream> _dataStreams = new Dictionary<int, Stream>();
        
        // /// <summary>Jenkins hash calulator</summary>
        // private static readonly Jenkins96 Hasher = new Jenkins96();
        
        /// <summary>Encoding file handler</summary>
        public readonly EncodingHandler EncodingHandler;
        
        // /// <summary>Download file handler</summary>
        // public readonly DownloadHandler DownloadHandler;
        
        /// <summary>Root file handler</summary>
        public readonly RootHandler RootHandler;

        /// <summary>Config for this handler</summary>
        public readonly CASCConfig Config;

        private CASCHandler(CASCConfig config, BackgroundWorkerEx worker) {
            // old CASCHandlerBase
            Config = config;

            if (!config.OnlineMode) {
                Debugger.Log(0, "CASC", "CASCHandler: loading local indices\r\n");

                using (PerfCounter _ = new PerfCounter("LocalIndexHandler.Initialize()")) {
                    _localIndex = LocalIndexHandler.Initialize(config, worker);
                }

                Debugger.Log(0, "CASC", $"CASCHandler: loaded {_localIndex.Count} local indexes\r\n");
            } else {  // todo: supposed to do this?
                Debugger.Log(0, "CASC", "CASCHandler: loading CDN indices\r\n");

                using (PerfCounter _ = new PerfCounter("CDNIndexHandler.Initialize()")) {
                    _cdnIndex = CDNIndexHandler.Initialize(config, worker);
                }

                Debugger.Log(0, "CASC", $"CASCHandler: loaded {_cdnIndex.Count} CDN indexes\r\n");
            }

            // old CASCHandler
            Debugger.Log(0, "CASC", "CASCHandler: loading encoding data\r\n");

            using (PerfCounter _ = new PerfCounter("new EncodingHandler()")) {
                using (BinaryReader fs = OpenEncodingFile()) {
                    EncodingHandler = new EncodingHandler(fs, worker);
                }
            }

            Debugger.Log(0, "CASC", $"CASCHandler: loaded {EncodingHandler.Count} encoding data\r\n");

            //if ((CASCConfig.LoadFlags & LoadFlags.Download) != 0)
            //{
            //    Debugger.Log(0, "CASC", "CASCHandler: loading download data\r\n");
            //    using (var _ = new PerfCounter("new DownloadHandler()"))
            //    {
            //        using (BinaryReader fs = OpenDownloadFile(EncodingHandler))
            //            DownloadHandler = new DownloadHandler(fs, worker);
            //    }
            //    Debugger.Log(0, "CASC", $"CASCHandler: loaded {EncodingHandler.Count} download data\r\n");
            //}

            Debugger.Log(0, "CASC", "CASCHandler: loading root data\r\n");

            using (PerfCounter _ = new PerfCounter("new RootHandler()")) {
                using (BinaryReader fs = OpenRootFile(EncodingHandler)) {
                    RootHandler = new RootHandler(fs, worker, this);
                }
            }

            Debugger.Log(0, "CASC", $"CASCHandler: loaded {RootHandler.Count} root data\r\n");

            //if ((CASCConfig.LoadFlags & LoadFlags.Install) != 0) {
            //    Debugger.Log(0, "CASC", "CASCHandler: loading install data\r\n");
            //    using (var _ = new PerfCounter("new InstallHandler()"))
            //    {
            //        using (var fs = OpenInstallFile(EncodingHandler))
            //            InstallHandler = new InstallHandler(fs, worker);
            //        InstallHandler.Print();
            //    }
            //    Debugger.Log(0, "CASC", $"CASCHandler: loaded {InstallHandler.Count} install data\r\n");
            //}
        }

        /// <summary>Create a new handler</summary>
        public static CASCHandler Open(CASCConfig config, BackgroundWorkerEx worker = null) {
            return new CASCHandler(config, worker);
        }

        public bool GetEncodingEntry(ulong hash, out EncodingEntry enc) {  // todo: unused here?
            IEnumerable<RootEntry> rootInfos = RootHandler.GetEntries(hash);
            IEnumerable<RootEntry> rootEntries = rootInfos as RootEntry[] ?? rootInfos.ToArray();
            if (rootEntries.Any())
                return EncodingHandler.GetEntry(rootEntries.First().MD5, out enc);

            enc = default(EncodingEntry);
            return false;
        }


        //public Stream OpenFile(string name) => OpenFile(Hasher.ComputeHash(name));

        /* public Stream OpenFile(ulong hash) {
             if (GetEncodingEntry(hash, out EncodingEntry encInfo))
                 return OpenFile(encInfo.Key);
             
             if (RootHandler.GetEntry(hash, out RootEntry entry))
                 if ((entry.ContentFlags & ContentFlags.Bundle) != ContentFlags.None)
                     if (EncodingHandler.GetEntry(entry.pkgIndex.bundleContentKey, out encInfo))
                         using (Stream bundle = OpenFile(encInfo.Key)) {
                             MemoryStream ms = new MemoryStream();
 
                             bundle.Position = entry.pkgIndexRec.Offset;
                             bundle.CopyBytes(ms, entry.pkgIndexRec.Size);
 
                             return ms;
                         }

             //if (CASCConfig.ThrowOnFileNotFound)
             //    throw new FileNotFoundException($"{hash:X16}");
             return null;
        }*/

        /// <summary>Open a file strean from encoding hash</summary>
        public Stream OpenFile(MD5Hash key) {
            try {
                if (Config.OnlineMode)
                    return OpenFileOnline(key);
                return OpenFileLocal(key);
            } catch (Exception exc) when (!(exc is BLTEKeyException)) {
                throw;  // todo?
                //return OpenFileOnline(key);
            }
        }

        #region Local
        /// <summary>Open a local file strean from encoding hash</summary>
        private Stream OpenFileLocal(MD5Hash key) {
            Stream stream = GetLocalDataStream(key);

            return new BLTEStream(stream, key);
        }

        protected Stream GetLocalDataStream(MD5Hash key) {
            IndexEntry idxInfo = _localIndex.GetIndexInfo(key);
            if (idxInfo == null) Debugger.Log(0, "CASC", $"CASCHandler: Local index missing: {key.ToHexString()}\r\n");

            if (idxInfo == null)
                throw new Exception("local index missing");

            Stream dataStream = GetDataStream(idxInfo.Index);
            dataStream.Position = idxInfo.Offset;

            using (BinaryReader reader = new BinaryReader(dataStream, Encoding.ASCII, true)) {
                byte[] md5 = reader.ReadBytes(16);
                Array.Reverse(md5);

                if (!key.EqualsTo9(md5))
                    throw new Exception("local data corrupted");

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

        private Stream GetDataStream(int index) {
            if (_dataStreams.TryGetValue(index, out Stream stream))
                return stream;

            string dataFolder = CASCConfig.GetDataFolder();

            string dataFile = Path.Combine(Config.BasePath, dataFolder, "data", $"data.{index:D3}");

            stream = new FileStream(dataFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _dataStreams[index] = stream;

            return stream;
        }
        #endregion

        #region Online        
        /// <summary>Open an online file strean from encoding hash</summary>
        protected Stream OpenFileOnline(MD5Hash key) {
            IndexEntry idxInfo = _cdnIndex.GetIndexInfo(key);
            return OpenFileOnlineInternal(idxInfo, key);
        }
        
        protected Stream OpenFileOnlineInternal(IndexEntry idxInfo, MD5Hash key) {
            Stream s = null;
            foreach (string host in Config.CDNHosts) {
                try {
                    if (idxInfo != null) {
                        s = _cdnIndex.OpenDataFile(idxInfo, host);
                    } else {
                        s = _cdnIndex.OpenDataFileDirect(key, host);
                    }
                } catch {
                    continue;
                }
                if (s != null && s.Length > 0) {
                    break;
                }
            }
            if (s == null) {
                return null;
            }
            return new BLTEStream(s, key);
        }
        #endregion

        #region Internal CASC files
        /// <summary>Open Install file</summary>
        protected BinaryReader OpenInstallFile(EncodingHandler enc) {
            if (!enc.GetEntry(Config.InstallMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for install file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }

        /// <summary>Open Download file</summary>
        protected BinaryReader OpenDownloadFile(EncodingHandler enc) {
            if (!enc.GetEntry(Config.DownloadMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for download file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }

        /// <summary>Open Root file</summary>
        protected BinaryReader OpenRootFile(EncodingHandler enc) {
            if (!enc.GetEntry(Config.RootMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for root file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }

        /// <summary>Open Encoding file</summary>
        protected BinaryReader OpenEncodingFile() {
            return new BinaryReader(OpenFile(Config.EncodingKey));
        }
        
        #endregion
    }
}
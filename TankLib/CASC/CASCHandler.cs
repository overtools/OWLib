using System;
using System.Diagnostics;
using System.IO;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    /// <summary>
    /// Content Addressable Storage Container (CASC) handler
    /// </summary>
    public class CASCHandler {
        /// <summary>Local indices</summary>
        public readonly LocalIndexHandler LocalIndex;
        
        /// <summary>CDN (remote) indices</summary>
        public readonly CDNIndexHandler CDNIndex;
        
        /// <summary>Encoding file handler</summary>
        public readonly EncodingHandler EncodingHandler;
        
        // /// <summary>Download file handler</summary>
        // public readonly DownloadHandler DownloadHandler;
        
        /// <summary>Root file handler</summary>
        public readonly RootHandler RootHandler;

        /// <summary>Config for this handler</summary>
        public readonly CASCConfig Config;

        /// <summary>Cached data</summary>
        public static readonly Cache Cache = new Cache("CASCCache"); 

        private CASCHandler(CASCConfig config, ProgressReportSlave worker) {
            Config = config;

            if (!config.OnlineMode) {
                Debugger.Log(0, "CASC", "CASCHandler: loading local indices\r\n");

                using (PerfCounter _ = new PerfCounter("LocalIndexHandler.Initialize()")) {
                    LocalIndex = LocalIndexHandler.Initialize(config, worker);
                }

                Debugger.Log(0, "CASC", $"CASCHandler: loaded {LocalIndex.Count} local indices\r\n");
            } else {  // todo: supposed to do this?
                Debugger.Log(0, "CASC", "CASCHandler: loading CDN indices\r\n");

                using (PerfCounter _ = new PerfCounter("CDNIndexHandler.Initialize()")) {
                    CDNIndex = CDNIndexHandler.Initialize(config, worker, Cache);
                }

                Debugger.Log(0, "CASC", $"CASCHandler: loaded {CDNIndex.Count} CDN indexes\r\n");
            }
            
            Debugger.Log(0, "CASC", "CASCHandler: loading encoding entries\r\n");
            using (PerfCounter _ = new PerfCounter("new EncodingHandler()")) {
                using (BinaryReader encodingReader = OpenEncodingKeyFile()) {
                    EncodingHandler = new EncodingHandler(encodingReader, worker);
                }
            }
            Debugger.Log(0, "CASC", $"CASCHandler: loaded {EncodingHandler.Count} encoding entries\r\n");

            Debugger.Log(0, "CASC", "CASCHandler: loading root data\r\n");
            using (PerfCounter _ = new PerfCounter("new RootHandler()")) {
                using (BinaryReader rootReader = OpenRootKeyFile()) {
                    RootHandler = new RootHandler(rootReader, worker, this);
                }
            }

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
        public static CASCHandler Open(CASCConfig config, ProgressReportSlave worker = null) {
            return new CASCHandler(config, worker);
        }

        /// <summary>Open a file stream from encoding hash</summary>
        public Stream OpenFile(MD5Hash key) {
            try {
                return Config.OnlineMode ? OpenFileOnline(key) : OpenFileLocal(key);
            } catch (Exception exc) when (!(exc is BLTEKeyException)) {
                throw;  // todo?
            }
        }
        
        /// <summary>Open a file stream from encoding hash and skip proper loading</summary>
        public Stream OpenFileRaw(MD5Hash key) {
            try {
                return Config.OnlineMode ? OpenFileOnline(key) : GetLocalDataStream(key);  // todo: online support
            } catch (Exception exc) when (!(exc is BLTEKeyException)) {
                throw;  // todo?
            }
        }

        #region Local
        /// <summary>Open a local file strean from encoding hash</summary>
        protected BLTEStream OpenFileLocal(MD5Hash key) {
            Stream stream = GetLocalDataStream(key);

            return new BLTEStream(stream, key);
        }

        protected Stream GetLocalDataStream(MD5Hash key) {
            IndexEntry idxInfo = LocalIndex.GetIndexInfo(key);
            //if (idxInfo == null) Debugger.Log(0, "CASC", $"CASCHandler: Local index missing: {key.ToHexString()}\r\n");

            if (idxInfo == null)
                throw new LocalIndexMissingException();
            
            return LocalIndex.OpenIndexInfo(idxInfo, key);
        }
        #endregion

        #region Online
        /// <summary>Open an online file strean from encoding hash</summary>
        protected BLTEStream OpenFileOnline(MD5Hash key) {
            IndexEntry idxInfo = CDNIndex.GetIndexInfo(key);
            return OpenFileOnlineInternal(idxInfo, key);
        }
        
        protected BLTEStream OpenFileOnlineInternal(IndexEntry idxInfo, MD5Hash key) {
            Stream s = null;
            foreach (string host in Config.CDNHosts) {
                try {
                    if (idxInfo != null) {
                        s = CDNIndex.OpenDataFile(idxInfo, host);
                    } else {
                        s = CDNIndex.OpenDataFileDirect(key, host);
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
        protected BinaryReader OpenInstallKeyFile() {
            if (!EncodingHandler.GetEntry(Config.InstallMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for install file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }

        /// <summary>Open Download file</summary>
        protected BinaryReader OpenDownloadKeyFile() {
            if (!EncodingHandler.GetEntry(Config.DownloadMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for download file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }

        /// <summary>Open Root file</summary>
        protected BinaryReader OpenRootKeyFile() {
            if (!EncodingHandler.GetEntry(Config.RootMD5, out EncodingEntry encInfo))
                throw new FileNotFoundException("encoding info for root file missing!");

            return new BinaryReader(OpenFile(encInfo.Key));
        }
        
        /// <summary>Open Patch file</summary>
        public BinaryReader OpenPatchKeyFile() {
            return new BinaryReader(OpenFileRaw(Config.PatchMD5));
        }
        
        /// <summary>Open Encoding file</summary>
        public BinaryReader OpenEncodingKeyFile() {
            return new BinaryReader(OpenFile(Config.EncodingKey));
        }
        
        #endregion
    }
}
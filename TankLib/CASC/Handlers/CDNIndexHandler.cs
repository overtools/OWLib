using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using TankLib.CASC.Helpers;
using TankLib.CASC.Remote;

namespace TankLib.CASC.Handlers {
    public class IndexEntry {
        public int Index;
        public int Offset;
        public int Size;
    }

    public class CDNIndexHandler {
        private static readonly MD5HashComparer Comparer = new MD5HashComparer();
        private Dictionary<MD5Hash, IndexEntry> _cdnIndexData = new Dictionary<MD5Hash, IndexEntry>(Comparer);

        private CASCConfig _config;
        private ProgressReportSlave _worker;
        private SyncDownloader _downloader;
        private readonly Cache _cache;

        public int Count => _cdnIndexData.Count;

        private CDNIndexHandler(CASCConfig cascConfig, ProgressReportSlave worker, Cache cache) {
            _config = cascConfig;
            _worker = worker;
            _downloader = new SyncDownloader();
            _cache = cache;
        }

        public static CDNIndexHandler Initialize(CASCConfig config, ProgressReportSlave worker, Cache cache) {
            CDNIndexHandler handler = new CDNIndexHandler(config, worker, cache);

            worker?.ReportProgress(0, "Loading \"CDN indexes\"...");

            for (int i = 0; i < config.Archives.Count; i++) {
                string archive = config.Archives[i];

                if (config.OnlineMode)
                    handler.DownloadIndexFile(archive, i);
                else
                    try {
                        handler.OpenIndexFile(archive, i);
                    } catch {
                        handler.DownloadIndexFile(archive, i);
                    }

                worker?.ReportProgress((int) ((i + 1) / (float) config.Archives.Count * 100));
            }

            return handler;
        }

        private void ParseIndex(Stream stream, int i) {
            using (BinaryReader br = new BinaryReader(stream)) {
                stream.Seek(-12, SeekOrigin.End);
                int count = br.ReadInt32();
                stream.Seek(0, SeekOrigin.Begin);

                if (count * (16 + 4 + 4) > stream.Length)
                    throw new Exception("ParseIndex failed");

                for (int j = 0; j < count; ++j) {
                    MD5Hash key = br.Read<MD5Hash>();

                    if (key.IsZeroed()) // wtf?
                        key = br.Read<MD5Hash>();

                    if (key.IsZeroed()) // wtf?
                        throw new Exception("key.IsZeroed()");

                    IndexEntry entry = new IndexEntry {
                        Index = i,
                        Size = br.ReadInt32BE(),
                        Offset = br.ReadInt32BE()
                    };
                    _cdnIndexData.Add(key, entry);
                }
            }
        }

        private void DownloadIndexFile(string archive, int i) {
            foreach (string host in _config.CDNHosts) {
                string file = _config.CDNPath + "/data/" + archive.Substring(0, 2) + "/" + archive.Substring(2, 2) +
                              "/" + archive + ".index";
                string url = "http://" + host + "/" + file;
                try {
                    Stream stream = _cache.OpenCDNFile(file, url, false);
                    if (stream == null) {
                        stream = _downloader.OpenFile(url);
                        if (stream == null) continue;
                    }

                    ParseIndex(stream, i);
                } catch { }
            }
        }

        private void OpenIndexFile(string archive, int i) {
            try {
                string dataFolder = CASCConfig.GetDataFolder();

                string path = Path.Combine(_config.BasePath, dataFolder, "indices", archive + ".index");

                using (FileStream fs = File.OpenRead(path)) {
                    ParseIndex(fs, i);
                }
            } catch {
                throw new Exception("OpenFile failed!");
            }
        }

        public Stream OpenDataFile(IndexEntry entry, string cdnHost) {
            string archive = _config.Archives[entry.Index];

            string file = _config.CDNPath + "/data/" + archive.Substring(0, 2) + "/" + archive.Substring(2, 2) + "/" +
                          archive;
            string url = "http://" + cdnHost + "/" + file;

            Stream stream = _cache.OpenCDNFile(file, url, true);

            if (stream != null) {
                stream.Position = entry.Offset;
                MemoryStream ms = new MemoryStream(entry.Size);
                stream.CopyBytes(ms, entry.Size);
                ms.Position = 0;
                return ms;
            }

            //using (HttpClient client = new HttpClient())
            //{
            //    client.DefaultRequestHeaders.Range = new RangeHeaderValue(entry.Offset, entry.Offset + entry.Size - 1);

            //    var resp = client.GetStreamAsync(url).Result;

            //    MemoryStream ms = new MemoryStream(entry.Size);
            //    resp.CopyBytes(ms, entry.Size);
            //    ms.Position = 0;
            //    return ms;
            //}

            HttpWebRequest req = WebRequest.CreateHttp(url);
            //req.Headers[HttpRequestHeader.Range] = string.Format("bytes={0}-{1}", entry.Offset, entry.Offset + entry.Size - 1);
            req.AddRange(entry.Offset, entry.Offset + entry.Size - 1);
            using (HttpWebResponse resp = (HttpWebResponse) req.GetResponseAsync().Result) {
                MemoryStream ms = new MemoryStream(entry.Size);
                resp.GetResponseStream().CopyBytes(ms, entry.Size);
                ms.Position = 0;
                return ms;
            }
        }

        public Stream OpenDataFileDirect(MD5Hash key, string cdnHost) {
            string keyStr = key.ToHexString().ToLower();

            _worker?.ReportProgress(0, $"Downloading \"{keyStr}\" file...");

            string file = _config.CDNPath + "/data/" + keyStr.Substring(0, 2) + "/" + keyStr.Substring(2, 2) + "/" +
                          keyStr;
            string url = "http://" + cdnHost + "/" + file;

            Stream stream = _cache.OpenCDNFile(file, url, false);

            return stream ?? _downloader.OpenFile(url);
        }

        public static Stream OpenConfigFileDirect(CASCConfig cfg, string key) {
            foreach (string host in cfg.CDNHosts) {
                string file = cfg.CDNPath + "/config/" + key.Substring(0, 2) + "/" + key.Substring(2, 2) + "/" + key;
                string url = "http://" + host + "/" + file;

                try {
                    Stream stream = CASCHandler.Cache.OpenCDNFile(file, url, false);
                    if (stream != null) return stream;

                    return OpenFileDirect(url);
                } catch { }
            }

            throw new FileNotFoundException();
        }

        public static Stream OpenFileDirect(string url) {
            //using (HttpClient client = new HttpClient())
            //{
            //    var resp = client.GetStreamAsync(url).Result;

            //    MemoryStream ms = new MemoryStream();
            //    resp.CopyTo(ms);
            //    ms.Position = 0;
            //    return ms;
            //}

            HttpWebRequest req = WebRequest.CreateHttp(url);
            using (HttpWebResponse resp = (HttpWebResponse) req.GetResponseAsync().Result) {
                MemoryStream ms = new MemoryStream();
                resp.GetResponseStream().CopyTo(ms);
                ms.Position = 0;
                return ms;
            }
        }

        public IndexEntry GetIndexInfo(MD5Hash key) {
            if (!_cdnIndexData.TryGetValue(key, out IndexEntry result))
                Debugger.Log(0, "CASC", $"CDNIndexHandler: missing index: {key.ToHexString()}\r\n");

            return result;
        }

        public void Clear() {
            _cdnIndexData.Clear();
            _cdnIndexData = null;

            _config = null;
            _worker = null;
            _downloader = null;
        }
    }
}
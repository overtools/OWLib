using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using CMFLib;
using TankLib.CASC.Remote;

namespace TankLib.CASC {
    public class CacheMetaData {
        public long Size { get; }
        public byte[] MD5 { get; }

        public CacheMetaData(long size, byte[] md5) {
            Size = size;
            MD5 = md5;
        }

        public void Save(string file) {
            File.WriteAllText(file + ".dat", $"{Size} {MD5.ToHexString()}");
        }

        public static CacheMetaData Load(string file) {
            if (File.Exists(file + ".dat")) {
                string[] tokens = File.ReadAllText(file + ".dat").Split(' ');
                return new CacheMetaData(Convert.ToInt64(tokens[0]), tokens[1].ToByteArray());
            }

            return null;
        }

        public static CacheMetaData AddToCache(HttpWebResponse resp, string file) {
            if (!resp.Headers[HttpResponseHeader.ETag].Contains(":")) {
                return null;
            }
            string md5 = resp.Headers[HttpResponseHeader.ETag].Split(':')[0].Substring(1);
            CacheMetaData meta = new CacheMetaData(resp.ContentLength, md5.ToByteArray());
            meta.Save(file);
            return meta;
        }
    }

    public class Cache {
        public bool CacheCDN = true;
        public bool CacheCDNData = true;
        public bool ValidateCDN = false;

        public bool CacheAPM = true;
        
        private readonly string _cachePath;
        
        public readonly string CDNCachePath;
        public readonly string APMCachePath;
        
        private readonly SyncDownloader _downloader = new SyncDownloader(null);

        private readonly MD5 _md5 = MD5.Create();

        public Cache(string path) {
            _cachePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), path);
            CDNCachePath = Path.Combine(_cachePath, "CDN");
            APMCachePath = Path.Combine(_cachePath, "APM");
            
            if (CacheCDN) {
                if (!Directory.Exists(CDNCachePath)) {
                    Directory.CreateDirectory(CDNCachePath);
                }
                Console.Out.WriteLine("CASC Cache path is {0}", _cachePath);
            }

            if (CacheAPM) {
                if (!Directory.Exists(APMCachePath)) {
                    Directory.CreateDirectory(APMCachePath);
                }
            }
        }

        public Stream OpenCDNFile(string name, string url, bool isData) {
            if (!CacheCDN)
                return null;

            if (isData && !CacheCDNData)
                return null;

            string file = Path.Combine(_cachePath, name);

            
            Debugger.Log(0, "CASC", $"CDNCache: Opening file {file}\r\n");

            FileInfo fi = new FileInfo(file);

            if (!fi.Exists || fi.Length == 0) {
                if (!_downloader.DownloadFile(url, file)) {
                    return null;
                }
            }

            if (ValidateCDN) {
                CacheMetaData meta = CacheMetaData.Load(file) ?? _downloader.GetMetaData(url, file);

                if (meta == null)
                    throw new Exception($"unable to validate file {file}");

                bool sizeOk, md5Ok;

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    sizeOk = fs.Length == meta.Size;
                    md5Ok = _md5.ComputeHash(fs).EqualsTo(meta.MD5);
                }

                if (!sizeOk || !md5Ok) {
                    if (!_downloader.DownloadFile(url, file)) {
                        return null;
                    }
                }
            }

            return new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public bool HasFile(string name) {
            return File.Exists(Path.Combine(_cachePath, name));
        }
    }
}
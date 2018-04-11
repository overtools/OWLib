using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using CMFLib;
using LZ4;
using TankLib.CASC.Remote;

namespace TankLib.CASC {
    public class Cache {
        public bool CacheCDN = true;
        public bool CacheCDNData = true;

        public bool CacheAPM = true;
        
        public readonly string CDNCachePath;
        public readonly string APMCachePath;
        
        private readonly string _cachePath;
        private readonly SyncDownloader _downloader;

        public Cache(string path) {
            _cachePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), path);
            CDNCachePath = Path.Combine(_cachePath, "CDN");
            APMCachePath = Path.Combine(_cachePath, "APM");
            
            _downloader = new SyncDownloader();
            
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
            
            using (Stream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (LZ4Stream lz4Stream = new LZ4Stream(fs, LZ4StreamMode.Decompress)) {
                return lz4Stream;
            }
        }

        public bool HasFile(string name) {
            return File.Exists(Path.Combine(_cachePath, name));
        }
    }
}
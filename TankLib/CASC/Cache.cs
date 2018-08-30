using System.Diagnostics;
using System.IO;
using LZ4;
using TankLib.CASC.Remote;

namespace TankLib.CASC {
    public class Cache {
        public bool CacheCDN = true;
        public bool CacheCDNData = true;

        public bool CacheAPM = true;
        
        public readonly string CDNCachePath;
        public readonly string APMCachePath;

        private readonly SyncDownloader _downloader;

        public Cache(string path) {
            string cachePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), path);
            CDNCachePath = Path.Combine(cachePath, "CDN");
            APMCachePath = Path.Combine(cachePath, "APM");
            
            _downloader = new SyncDownloader();
            
            if (CacheCDN) {
                if (!Directory.Exists(CDNCachePath)) {
                    Directory.CreateDirectory(CDNCachePath);
                }
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

            string file = Path.Combine(CDNCachePath, name);

            
            Debugger.Log(0, "CASC", $"CDNCache: Opening file {file}\r\n");

            FileInfo fi = new FileInfo(file);

            if (!fi.Exists || fi.Length == 0) {
                if (!_downloader.DownloadFile(url, file)) {
                    return null;
                }
            }

            Stream fs = File.OpenRead(file);
            return new LZ4Stream(fs, LZ4StreamMode.Decompress);
        }

        public bool HasFile(string name) {
            return File.Exists(Path.Combine(CDNCachePath, name));
        }
    }
}
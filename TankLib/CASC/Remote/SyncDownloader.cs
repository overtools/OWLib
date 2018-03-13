using System.Diagnostics;
using System.IO;
using System.Net;
using TankLib.CASC.Helpers;

namespace TankLib.CASC.Remote {
    public class SyncDownloader {
        private readonly BackgroundWorkerEx _progressReporter;

        public SyncDownloader(BackgroundWorkerEx progressReporter) {
            _progressReporter = progressReporter;
        }

        public bool DownloadFile(string url, string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            
            Debugger.Log(0, "CASC", $"SyncDownloader: Downloading file {url}\r\n");

            HttpWebRequest request = WebRequest.CreateHttp(url);

            using (HttpWebResponse resp = (HttpWebResponse) request.GetResponseAsync().Result) {
                if (resp.ContentLength == 0) return false;

                if (resp.Headers[HttpResponseHeader.ETag] == null || resp.StatusCode != HttpStatusCode.OK) return false;

                using (Stream stream = resp.GetResponseStream()) {
                    if (stream == null) return false;

                    using (Stream fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                        CacheMetaData.AddToCache(resp, path);
                        CopyToStream(stream, fs, resp.ContentLength);
                        return true;
                    }
                }
            }
        }

        public MemoryStream OpenFile(string url) {
            HttpWebRequest request = WebRequest.CreateHttp(url);

            using (HttpWebResponse resp = (HttpWebResponse) request.GetResponseAsync().Result) {
                if (resp.ContentLength == 0) return null;

                if (resp.Headers[HttpResponseHeader.ETag] == null || resp.StatusCode != HttpStatusCode.OK) return null;

                using (Stream stream = resp.GetResponseStream()) {
                    MemoryStream ms = new MemoryStream();

                    CopyToStream(stream, ms, resp.ContentLength);

                    ms.Position = 0;
                    return ms;
                }
            }
        }

        public CacheMetaData GetMetaData(string url, string file) {
            try {
                HttpWebRequest request = WebRequest.CreateHttp(url);
                request.Method = "HEAD";

                using (HttpWebResponse resp = (HttpWebResponse) request.GetResponseAsync().Result) {
                    return CacheMetaData.AddToCache(resp, file);
                }
            } catch {
                return null;
            }
        }

        private void CopyToStream(Stream src, Stream dst, long len) {
            long done = 0;

            byte[] buf = new byte[0x1000];

            int count;
            do {
                if (_progressReporter != null && _progressReporter.CancellationPending)
                    return;

                count = src.Read(buf, 0, buf.Length);
                dst.Write(buf, 0, count);

                done += count;

                _progressReporter?.ReportProgress((int) (done / (float) len * 100));
            } while (count > 0);
        }
    }
}
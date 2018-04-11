using System.IO;
using System.Net;
using LZ4;

namespace TankLib.CASC.Remote {
    public class SyncDownloader {
        public bool DownloadFile(string url, string path) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (Stream stream = OpenFile(url)) {
                if (stream == null) return false;

                using (Stream fs = new FileStream(path, FileMode.Create, FileAccess.Write)) {
                    using (LZ4Stream lz4Stream = new LZ4Stream(fs, LZ4StreamMode.Compress))
                        stream.CopyTo(lz4Stream);
                    return true;
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

        private void CopyToStream(Stream src, Stream dst, long len) {
            byte[] buf = new byte[0x1000];

            int count;
            do {
                count = src.Read(buf, 0, buf.Length);
                dst.Write(buf, 0, count);
            } while (count > 0);
        }
    }
}
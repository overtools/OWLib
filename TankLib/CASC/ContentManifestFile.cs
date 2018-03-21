using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CMFLib;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    /// <summary>CMF file</summary>
    public class ContentManifestFile {
        /// <summary>Header data</summary>
        public readonly CMFHeader Header;
        
        public List<CMFHashData> HashList;
        
        public List<CMFEntry> Entries;
        
        public Dictionary<ulong, CMFHashData> Map;
        
        /// <summary>Read teh file</summary>
        /// <param name="name">APM name</param>
        /// <param name="stream">Source stream</param>
        /// <param name="worker">Background worker</param>
        public ContentManifestFile(string name, Stream stream, BackgroundWorkerEx worker) {
            using (BinaryReader cmfreader = new BinaryReader(stream)) {
                stream.Seek(0, SeekOrigin.Begin);
                ulong cmfVersion = cmfreader.ReadUInt64();
                stream.Seek(0, SeekOrigin.Begin);
                if (cmfVersion >= 39028) {
                    Header = cmfreader.Read<CMFHeader>();
                }
                else {
                    Header = cmfreader.Read<CMFHeader17>().Upgrade();
                }
                worker?.ReportProgress(0, $"Loading CMF {name}...");
                
                if (Header.Magic >= 0x636D6614) {
                    using (BinaryReader decryptedReader = DecryptCMF(cmfreader, Path.GetFileName(name))) {
                        ParseCMF(decryptedReader);
                    }
                }
                else {
                    ParseCMF(cmfreader);
                }
            }
        }
        
        internal void ParseCMF(BinaryReader cmfreader) {
            Entries = new List<CMFEntry>((int) Header.EntryCount);
            for (uint i = 0; i < Header.EntryCount; i++) {
                CMFEntry a = cmfreader.Read<CMFEntry>();
                Entries.Add(a);
            }

            HashList = new List<CMFHashData>((int) Header.DataCount);
            Map = new Dictionary<ulong, CMFHashData>((int) Header.DataCount);
            for (uint i = 0; i < Header.DataCount; i++) {
                CMFHashData a = cmfreader.Read<CMFHashData>();
                HashList.Add(a);
                Map[a.id] = a;
            }
        }

        private BinaryReader DecryptCMF(BinaryReader cmfreader, string name) {
            KeyValuePair<byte[], byte[]> keyIV = CMFHandler.GenerateKeyIV(name, Header, CMFApplication.Prometheus); // todo: support other app types
            using (RijndaelManaged rijndael = new RijndaelManaged {Key = keyIV.Key, IV = keyIV.Value, Mode = CipherMode.CBC}) {
                CryptoStream cryptostream = new CryptoStream(cmfreader.BaseStream, rijndael.CreateDecryptor(),
                    CryptoStreamMode.Read);
                return new BinaryReader(cryptostream);
            }
        }
    }
}
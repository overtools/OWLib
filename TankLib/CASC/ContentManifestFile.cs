using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CMFLib;
using TankLib.CASC.Helpers;

namespace TankLib.CASC {
    /// <summary>CMF file</summary>
    public class ContentManifestFile {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct HashData {
            public ulong GUID;
            public uint Size;
            public MD5Hash HashKey;
        }
        
        /// <summary>Header data</summary>
        public readonly CMFHeader Header;
        
        public HashData[] HashList;
        public ApplicationPackageManifest.Types.Entry[] Entries;
        public Dictionary<ulong, HashData> Map;
        
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
            Entries = cmfreader.ReadArray<ApplicationPackageManifest.Types.Entry>((int)Header.EntryCount);
            
            HashList = cmfreader.ReadArray<HashData>((int)Header.DataCount);
            Map = new Dictionary<ulong, HashData>((int)Header.DataCount);
            for (uint i = 0; i < (int)Header.DataCount; i++) {
                Map[HashList[i].GUID] = HashList[i];
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
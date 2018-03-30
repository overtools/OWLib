using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public CMFHeaderCommon Header;
        
        public HashData[] HashList;
        public ApplicationPackageManifest.Types.Entry[] Entries;
        public Dictionary<ulong, HashData> Map;
        
        /// <summary>Read a CMF file</summary>
        /// <param name="name">APM name</param>
        /// <param name="stream">Source stream</param>
        /// <param name="worker">Background worker</param>
        public ContentManifestFile(string name, Stream stream, BackgroundWorkerEx worker) {
            //using (Stream file = File.OpenWrite(Path.GetFileName(name))) {
            //    stream.CopyTo(file);
            //    stream.Position = 0;
            //}
            
            //Entries = new ApplicationPackageManifest.Types.Entry[0];
            //Map = new Dictionary<ulong, HashData>(0);
            //HashList = new HashData[0];
            //return;
            
            using (BinaryReader reader = new BinaryReader(stream)) {
                Read(reader, name, worker);
            }
        }

        protected ContentManifestFile() {}

        protected void Read(BinaryReader reader, string name, BackgroundWorkerEx worker=null) {
            reader.BaseStream.Position = 0;
            uint cmfVersion = reader.ReadUInt32();
            reader.BaseStream.Position = 0;
                
            // todo: the int of Header21 is converted to uint without checking
                
            if (cmfVersion >= 45104) {
                Header = reader.Read<CMFHeader22>().Upgrade();
            } else if (cmfVersion >= 39028) {
                Header = reader.Read<CMFHeader21>().Upgrade();
            } else {
                Header = reader.Read<CMFHeader20>().Upgrade();
            }
            worker?.ReportProgress(0, $"Loading CMF {name}...");
                
            if (Header.Magic >= 0x636D6614) {
                using (BinaryReader decryptedReader = DecryptCMF(reader, name)) {
                    ParseCMF(decryptedReader);
                }
            } else {
                ParseCMF(reader);
            }
        }

        protected void ParseCMF(BinaryReader cmfreader) {
            if (Header.BuildVersion >= 45104) {
                Entries = cmfreader.ReadArray<ApplicationPackageManifest.Types.Entry>((int)Header.EntryCount);
            } else {
                Entries = cmfreader.ReadArray<ApplicationPackageManifest.Types.Entry21>((int)Header.EntryCount).Select(x => x.GetEntry()).ToArray();
            }
            
            HashList = cmfreader.ReadArray<HashData>((int)Header.DataCount);
            Map = new Dictionary<ulong, HashData>((int)Header.DataCount);
            for (uint i = 0; i < (int)Header.DataCount; i++) {
                Map[HashList[i].GUID] = HashList[i];
            }
        }

        protected virtual BinaryReader DecryptCMF(BinaryReader cmfreader, string name) {
            KeyValuePair<byte[], byte[]> keyIV = CMFHandler.GenerateKeyIV(name, Header, CMFApplication.Prometheus); // todo: support other app types
            using (RijndaelManaged rijndael = new RijndaelManaged {Key = keyIV.Key, IV = keyIV.Value, Mode = CipherMode.CBC}) {
                CryptoStream cryptostream = new CryptoStream(cmfreader.BaseStream, rijndael.CreateDecryptor(),
                    CryptoStreamMode.Read);
                return new BinaryReader(cryptostream);
            }
        }
    }
}
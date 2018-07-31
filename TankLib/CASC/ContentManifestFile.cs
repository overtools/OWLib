using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
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
        public Dictionary<ulong, int> IndexMap;
        
        /// <summary>Read a CMF file</summary>
        /// <param name="name">APM name</param>
        /// <param name="stream">Source stream</param>
        /// <param name="worker">Background worker</param>
        public ContentManifestFile(string name, Stream stream, ProgressReportSlave worker) {
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

        // ReSharper disable once InconsistentNaming
        public static readonly int ENCRYPTED_MAGIC = Util.GetMagicBytes('c', 'm', 'f');

        protected void Read(BinaryReader reader, string name, ProgressReportSlave worker=null) {
            reader.BaseStream.Position = 0;
            uint cmfVersion = reader.ReadUInt32();
            reader.BaseStream.Position = 0;
                
            // todo: the int of Header21 is converted to uint without checking
                
            if (CMFHeaderCommon.IsV22(cmfVersion)) {
                Header = reader.Read<CMFHeader22>().Upgrade();
            } else if (cmfVersion >= 39028) {
                Header = reader.Read<CMFHeader21>().Upgrade();
            } else {
                Header = reader.Read<CMFHeader20>().Upgrade();
            }
            worker?.ReportProgress(0, $"Loading CMF {name}...");
                
            if (Header.Magic >> 8 == ENCRYPTED_MAGIC) {
                using (BinaryReader decryptedReader = DecryptCMF(reader, name)) {
                    ParseCMF(decryptedReader);
                }
            } else {
                ParseCMF(reader);
            }
        }

        protected void ParseCMF(BinaryReader cmfreader) {
            if (Header.IsV22()) {
                Entries = cmfreader.ReadArray<ApplicationPackageManifest.Types.Entry>((int)Header.EntryCount);
            } else {
                Entries = cmfreader.ReadArray<ApplicationPackageManifest.Types.Entry21>((int)Header.EntryCount).Select(x => x.GetEntry()).ToArray();
            }
            
            HashList = cmfreader.ReadArray<HashData>((int)Header.DataCount);
            
            Map = new Dictionary<ulong, HashData>((int)Header.DataCount);
            IndexMap = new Dictionary<ulong, int>((int)Header.DataCount);
            for (uint i = 0; i < (int)Header.DataCount; i++) {
                Map[HashList[i].GUID] = HashList[i];
                IndexMap[HashList[i].GUID] = (int)i;
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
    
    public class CMFHandler {
        #region Helpers
        // ReSharper disable once InconsistentNaming
        internal const uint SHA1_DIGESTSIZE = 20;
        
        internal static uint Constrain(long value) {
            return (uint)(value % uint.MaxValue);
        }
        
        internal static long SignedMod(long a, long b) {
            return a % b < 0 ? a % b + b : a % b;
        }
        #endregion
        
        private static readonly Dictionary<CMFApplication, Dictionary<uint, ICMFProvider>> Providers = new Dictionary<CMFApplication, Dictionary<uint, ICMFProvider>>();
        
        private static void FindProviders(CMFApplication app) {
            Providers[app] = new Dictionary<uint, ICMFProvider>();
            Assembly asm = typeof(ICMFProvider).Assembly;
            AddProviders(asm);
        }
        
        public static KeyValuePair<byte[], byte[]> GenerateKeyIV(string name, CMFHeaderCommon header, CMFApplication app) {
            if (!Providers.ContainsKey(app)) {
                FindProviders(app);
            }

            byte[] digest = CreateDigest(name);

            ICMFProvider provider;
            if (Providers[app].ContainsKey(header.BuildVersion)) {
                TankLib.Helpers.Logger.Info("CASC", $"Using CMF procedure {header.BuildVersion}");
                provider = Providers[app][header.BuildVersion];
            } else {
                TankLib.Helpers.Logger.Warn("CASC", $"No CMF procedure for build {header.BuildVersion}, trying closest version");
                try {
                    KeyValuePair<uint, ICMFProvider> pair = Providers[app].Where(it => it.Key < header.BuildVersion).OrderByDescending(it => it.Key).First();
                    TankLib.Helpers.Logger.Info("CASC", $"Using CMF procedure {pair.Key}");
                    provider = pair.Value;
                } catch {
                    throw new CryptographicException("Missing CMF generators");
                }
            }

            byte[] key = provider.Key(header, name, digest, 32);
            byte[] iv = provider.IV(header, name, digest, 16);

            name = Path.GetFileNameWithoutExtension(name);
            TankLib.Helpers.Logger.Debug("CMF", $"{name}: key={string.Join(" ", key.Select(x => x.ToString("X2")))}");
            TankLib.Helpers.Logger.Debug("CMF", $"{name}: iv={string.Join(" ", iv.Select(x => x.ToString("X2")))}");
            return new KeyValuePair<byte[], byte[]>(key, iv);
        }
        
        private static byte[] CreateDigest(string value) {
            byte[] digest;
            using (SHA1 shaM = new SHA1Managed()) {
                byte[] stringBytes = Encoding.ASCII.GetBytes(value);
                digest = shaM.ComputeHash(stringBytes);
            }
            return digest;
        }

        public static void AddProviders(Assembly asm) {
            Type t = typeof(ICMFProvider);
            List<Type> types = asm.GetTypes().Where(tt => tt != t && t.IsAssignableFrom(tt)).ToList();
            foreach (Type tt in types) {
                if (tt.IsInterface) {
                    continue;
                }
                CMFMetadataAttribute metadata = tt.GetCustomAttribute<CMFMetadataAttribute>();
                if (metadata == null) {
                    continue;
                }

                if (!Providers.ContainsKey(metadata.App)) {
                    Providers[metadata.App] = new Dictionary<uint, ICMFProvider>();
                }
                ICMFProvider provider = (ICMFProvider)Activator.CreateInstance(tt);
                if (metadata.AutoDetectVersion) {
                    Providers[metadata.App][uint.Parse(tt.Name.Split('_')[1])] = provider;
                }

                if (metadata.BuildVersions != null) {
                    foreach (uint buildVersion in metadata.BuildVersions) {
                        Providers[metadata.App][buildVersion] = provider;
                    }
                }
            }
        }
    }
}
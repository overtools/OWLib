using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CMFLib;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.ExportFormats;
using TankLib.STU;
using TankLib.STU.Types;

namespace CASCEncDump {
    internal class Program {
        public static Dictionary<ulong, MD5Hash> Files;
        public static CASCConfig Config;
        public static CASCHandler CASC;
        public static uint BuildVersion;
        
        public static string RawIdxDir => $"dump\\{BuildVersion}\\idx\\raw";
        public static string RawEncDir => $"dump\\{BuildVersion}\\enc\\raw";
        public static string ConvertIdxDir => $"dump\\{BuildVersion}\\idx\\convert";
        public static string ConvertEncDir => $"dump\\{BuildVersion}\\enc\\convert";
        public static string NonBLTEDir => $"dump\\{BuildVersion}\\nonblte";
        public static string KeyFilesDir => $"dump\\{BuildVersion}\\keyfiles";
        public static string AllCMFDir => $"dump\\{BuildVersion}\\allcmf";
        public static string GUIDDir => $"dump\\{BuildVersion}\\guids";
        
        public static void Main(string[] args) {
            string overwatchDir = args[0];
            string mode = args[1];
            const string language = "enUS";
            
            // Usage:
            // {overwatch dir} dump  --  Dump hashes
            // {overwatch dir} compare-enc {other ver num}  --  Extract added files from encoding (requires dump from other version)
            // {overwatch dir} compare-idx {other ver num}  --  Extract added files from indices (requires dump from other version)
            // {overwatch dir} nonblte  --  Extract non-blte files
            // {overwatch dir} extract-encoding  --  Extract encoding file
            // {overwatch dir} addcmf  --  Extract all files from the cmf

            // casc setup
            Config = CASCConfig.LoadLocalStorageConfig(overwatchDir, false, false);
            Config.SpeechLanguage = Config.TextLanguage = language;
            if (mode != "allcmf" && mode != "dump-guids" && mode != "compare-guids" && mode != "dump-cmf") {
                Config.LoadContentManifest = false;
                Config.LoadPackageManifest = false;
            }
            
            CASC = CASCHandler.Open(Config);
            MapCMF(language);

            //var temp = Config.Builds[Config.ActiveBuild].KeyValue;
            BuildVersion = uint.Parse(Config.BuildVersion.Split('.').Last());

            // c:\\ow\\game\\Overwatch dump
            // "D:\Games\Overwatch Test" compare 44022

            if (mode == "dump") {
                Dump(args);
            } else if (mode == "compare-enc") {
                CompareEnc(args);
            } else if (mode == "compare-idx") {
                CompareIdx(args);
            } else if (mode == "nonblte") {
                DumpNonBLTE(args);
            } else if (mode == "extract-encoding") {
                ExtractEncodingFile(args);
            } else if (mode == "allcmf") {
                AllCMF(args);
            } else if (mode == "dump-guids") {
                DumpGUIDs(args);
            } else if (mode == "compare-guids") {
                CompareGUIDs(args); 
            } else if (mode == "dump-cmf") {
                DumpCMF(args);
            } else {
                throw new Exception($"unknown mode: {mode}");
            }
        }

        public static void DumpCMF(string[] args) {
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.cmfhashes")) {
                foreach (KeyValuePair<ulong,MD5Hash> file in Files) {
                    writer.WriteLine(file.Value.ToHexString());
                }
            }
        }

        public static void DumpGUIDs(string[] args) {
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.guids")) {
                foreach (KeyValuePair<ulong,MD5Hash> file in Files) {
                    writer.WriteLine(file.Key.ToString("X"));
                }
            }
        }

        public static void CompareGUIDs(string[] args) {
            string otherVerNum = args[2];

            Directory.CreateDirectory(GUIDDir);  // file name is the vesion it is compared to

            ulong[] last;
            using (StreamReader reader = new StreamReader($"{otherVerNum}.guids")) {
                last = reader.ReadToEnd().Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => ulong.Parse(x, NumberStyles.HexNumber)).ToArray();
            }

            List<ulong> added = Files.Keys.Except(last).ToList();
            List<ulong> removed = last.Except(Files.Keys).ToList();
            
            using (StreamWriter writer = new StreamWriter(Path.Combine(GUIDDir, $"{otherVerNum}.added"))) {
                foreach (ulong addedFile in added) {
                    writer.WriteLine(teResourceGUID.AsString(addedFile));
                }
            }
            
            using (StreamWriter writer = new StreamWriter(Path.Combine(GUIDDir, $"{otherVerNum}.removed"))) {
                foreach (ulong removedFile in removed) {
                    writer.WriteLine(teResourceGUID.AsString(removedFile));
                }
            }
        }

        public static void AllCMF(string[] args) {
            ushort[] types = args.Skip(2).Select(x => ushort.Parse(x, NumberStyles.HexNumber)).ToArray();
            
            Directory.CreateDirectory(AllCMFDir);
            foreach (KeyValuePair<ulong,MD5Hash> cmfFile in Files) {
                if (!types.Contains(teResourceGUID.Type(cmfFile.Key))) continue;
                try {
                    using (Stream stream = OpenFile(cmfFile.Key)) {
                        if (stream == null) continue;
                        string typeDir = Path.Combine(AllCMFDir, teResourceGUID.Type(cmfFile.Key).ToString("X3"));
                        Directory.CreateDirectory(typeDir);
                        using (Stream file = File.OpenWrite(Path.Combine(typeDir, teResourceGUID.AsString(cmfFile.Key)))) {
                            stream.CopyTo(file);
                        }
                    }
                } catch (Exception e) {
                    Console.Out.WriteLine(e);
                }
            }
        }

        public static void ExtractEncodingFile(string[] args) {
            Directory.CreateDirectory(KeyFilesDir);
            using (BinaryReader reader = CASC.OpenEncodingKeyFile()) {
                using (Stream file = File.OpenWrite(Path.Combine(KeyFilesDir, "encoding"))) {
                    reader.BaseStream.CopyTo(file);
                }
            }
        }

        public static void Dump(string[] args) {
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.enchashes")) {
                foreach (KeyValuePair<MD5Hash,EncodingEntry> entry in CASC.EncodingHandler.Entries) {
                    string md5 = entry.Key.ToHexString();
                    
                    writer.WriteLine(md5);
                }
            }
            
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.idxhashes")) {
                foreach (KeyValuePair<MD5Hash, IndexEntry> entry in CASC.LocalIndex.Indices) {
                    string md5 = entry.Key.ToHexString();
                    
                    writer.WriteLine(md5);
                }
            }
        }

        public static void DumpNonBLTE(string[] args) {
            Directory.CreateDirectory(NonBLTEDir);
            foreach (KeyValuePair<MD5Hash, IndexEntry> indexEntry in CASC.LocalIndex.Indices) {
                string md5 = indexEntry.Key.ToHexString();
                MD5Hash md5Obj = new MD5Hash();

                try {
                    Stream rawStream = CASC.LocalIndex.OpenIndexInfo(indexEntry.Value, md5Obj, false);

                    using (BinaryReader reader = new BinaryReader(rawStream)) {
                        uint magic = reader.ReadUInt32();

                        if (magic == BLTEStream.BLTEMagic) continue;

                        rawStream.Position = 0;

                        using (Stream file = File.OpenWrite(Path.Combine(NonBLTEDir, md5) + ".nonblte")) {
                            rawStream.CopyTo(file);
                        }
                    }
                } catch (Exception e) {
                    Console.Out.WriteLine(e);
                }
            }
        }

        public static void CompareIdx(string[] args) {
            string otherVerNum = args[2];
            
            HashSet<ulong> missingKeys = new HashSet<ulong>();

            Directory.CreateDirectory(RawIdxDir);
            Directory.CreateDirectory(ConvertIdxDir);

            string[] otherHashes;
            using (StreamReader reader = new StreamReader($"{otherVerNum}.idxhashes")) {
                otherHashes = reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
            }
            
            MD5Hash md5Obj = new MD5Hash();

            foreach (KeyValuePair<MD5Hash,IndexEntry> indexEntry in CASC.LocalIndex.Indices) {
                string md5 = indexEntry.Key.ToHexString();

                if (!otherHashes.Contains(md5)) {
                    try {
                        Stream rawStream = CASC.LocalIndex.OpenIndexInfo(indexEntry.Value, md5Obj, false);
                        
                        Stream stream = new BLTEStream(rawStream, md5Obj);
                        
                        TryConvertFile(stream, ConvertIdxDir, md5);

                        //stream.Position = 0;
                        //using (Stream file = File.OpenWrite(Path.Combine(RawIdxDir, md5))) {
                        //    stream.CopyTo(file);
                        //}
                        
                        rawStream.Dispose();
                        stream.Dispose();
                    } catch (Exception e) {
                        if (e is BLTEKeyException exception) {
                            if (missingKeys.Add(exception.MissingKey)) {
                                Console.Out.WriteLine($"Missing key: {exception.MissingKey:X16}");
                            }
                        } 
                        //else {
                        //    Console.Out.WriteLine(e);
                        //}
                    }
                }
            }

            Console.Write("done");
            Console.ReadLine();
        }

        public static void CompareEnc(string[] args) {
            string otherVerNum = args[2];
            
            HashSet<ulong> missingKeys = new HashSet<ulong>();

            Directory.CreateDirectory(RawEncDir);
            Directory.CreateDirectory(ConvertEncDir);

            string[] otherHashes;
            using (StreamReader reader = new StreamReader($"{otherVerNum}.enchashes")) {
                otherHashes = reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }

            Dictionary<MD5Hash, int> otherHashDict = new Dictionary<MD5Hash, int>(new MD5HashComparer());
            foreach (MD5Hash hash in otherHashes.Select(x => x.ToByteArray().ToMD5())) {
                otherHashDict[hash] = 0;
            }

            foreach (KeyValuePair<MD5Hash, EncodingEntry> entry in CASC.EncodingHandler.Entries) {
                string md5 = entry.Key.ToHexString();

                if (!otherHashDict.ContainsKey(entry.Key)) {
                    try {
                        Stream stream = CASC.OpenFile(entry.Value.Key);
                        
                        TryConvertFile(stream, ConvertEncDir, md5);

                        //stream.Position = 0;
                        //using (Stream file = File.OpenWrite(Path.Combine(RawEncDir, md5))) {
                        //    stream.CopyTo(file);
                        //}
                    } catch (Exception e) {
                        if (e is BLTEKeyException exception) {
                            if (missingKeys.Add(exception.MissingKey)) {
                                Console.Out.WriteLine($"Missing key: {exception.MissingKey:X16}");
                            }
                        } else {
                            Console.Out.WriteLine(e);
                        }
                    }
                }
            }
        }

        public static void TryConvertFile(Stream stream, string convertDir, string md5) {
            //List<byte> lods = new List<byte>(new byte[3] { 0, 1, 0xFF });

            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                uint magic = reader.ReadUInt32();
                
                stream.Position = 0;
                if (magic == teChunkedData.Magic) {
                    teChunkedData chunkedData = new teChunkedData(reader);
                    if (chunkedData.Header.StringIdentifier == "MODL") {
                        OverwatchModel model = new OverwatchModel(chunkedData, 0);
                        using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".owmdl")) {
                            file.SetLength(0);
                            model.Write(file);
                        }
                    }
                } else if (magic == 0x4D4F5649) {  // MOVI
                    stream.Position = 128;
                    using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".bk2")) {
                        file.SetLength(0);
                        stream.CopyTo(file);
                    }
                } else {
                    // ok might be a heckin bundle
                    /*int i = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length) {
                        try {
                            magic = reader.ReadUInt32();
                            if (magic != teChunkedData.Magic) {
                                reader.BaseStream.Position -= 3;
                                continue;
                            }
                            reader.BaseStream.Position -= 4;
                            teChunkedData chunkedData = new teChunkedData(reader);
                            if (chunkedData.Header.StringIdentifier == "MODL") {
                                OverwatchModel model = new OverwatchModel(chunkedData);
                                using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + $"-{i}.owmdl")) {
                                    file.SetLength(0);
                                    model.Write(file);
                                }
                            }
    
                            i++;
                        } catch (Exception) {
                            // fine
                        }
                    }*/

                    try {
                        //teStructuredData structuredData =new teStructuredData(stream, true);
                        
                        teTexture texture = new teTexture(reader);
                        if (!texture.PayloadRequired && texture.Size <= stream.Length && 
                            (texture.Header.Type == TextureTypes.TEXTURE_FLAGS.CUBEMAP ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.DIFFUSE ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.MULTISURFACE ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.UNKNOWN1 ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.UNKNOWN2 ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.UNKNOWN4 ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.UNKNOWN5 ||
                             texture.Header.Type == TextureTypes.TEXTURE_FLAGS.WORLD) && 
                            texture.Header.Height < 10000 && texture.Header.Width < 10000 && texture.Header.DataSize > 68) {
                            using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".dds")) {
                                file.SetLength(0);
                                texture.SaveToDDS(file);
                            }
                        }
                    } catch (Exception) {
                        // fine
                    }

                    try {
                        stream.Position = 0;
                        teStructuredData structuredData =new teStructuredData(stream, true);

                        if (structuredData.GetInstance<STUResourceKey>() != null) {
                            var key = structuredData.GetInstance<STUResourceKey>();
                            
                            Console.Out.WriteLine("found key");
                            var longKey = ulong.Parse(key.m_keyID, NumberStyles.HexNumber);
                            var longRevKey = BitConverter.ToUInt64(BitConverter.GetBytes(longKey).Reverse().ToArray(), 0);
                            var keyValueString = BitConverter.ToString(key.m_key).Replace("-", string.Empty);
                            var keyNameProper = longRevKey.ToString("X16");
                            Console.Out.WriteLine("Added Encryption Key {0}, Value: {1}",keyNameProper, keyValueString);
                        }
                        if (structuredData.GetInstance<STUHero>() != null) {
                            
                        }
                    } catch (Exception) {
                        // fine
                    }
                }
            }
        }
        
        
        public static void MapCMF(string locale) {
            Files = new Dictionary<ulong, MD5Hash>();
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                const string searchString = "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + locale.ToLowerInvariant())) {
                    continue;
                }
                foreach (KeyValuePair<ulong, ContentManifestFile.HashData> pair in apm.CMF.Map) {
                    Files[pair.Value.GUID] = pair.Value.HashKey;
                }
            }
        }
        
        public static Stream OpenFile(ulong guid) {
            return OpenFile(CASC, Files[guid]);
        }
        
        public static Stream OpenFile(CASCHandler casc, MD5Hash hash) {
            try {
                return casc.EncodingHandler.GetEntry(hash, out EncodingEntry enc) ? casc.OpenFile(enc.Key) : null;
            } catch (Exception e) {
                if (e is BLTEKeyException exception) {
                    Debugger.Log(0, "TankLibTest:CASC", $"Missing key: {exception.MissingKey:X16}\r\n");
                }
                return null;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.ExportFormats;

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
            Config.Languages = new HashSet<string> {language};
            if (mode != "allcmf") {
                Config.LoadContentManifest = false;
                Config.LoadPackageManifest = false;
            }
            
            CASC = CASCHandler.Open(Config);
            MapCMF(language);

            //var temp = Config.Builds[Config.ActiveBuild].KeyValue;
            BuildVersion = uint.Parse(Config.BuildName.Split('.').Last());

            // c:\\ow\\game\\Overwatch dump
            // "D:\Games\Overwatch Test" compare 44022
            

            //using (BinaryReader reader = CASC.OpenPatchFile()) {
            //    char a = reader.ReadChar();
            //    char b = reader.ReadChar();
            //}

            if (mode == "dump") {
                DumpEnc(args);
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
            } else {
                throw new Exception($"unknown mode: {mode}");
            }
        }

        public static void AllCMF(string[] args) {
            Directory.CreateDirectory(AllCMFDir);
            foreach (KeyValuePair<ulong,MD5Hash> cmfFile in Files) {
                if (teResourceGUID.Type(cmfFile.Key) != 0x77) continue;  // todo
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

        public static void DumpEnc(string[] args) {
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.enchashes")) {
                foreach (KeyValuePair<MD5Hash,EncodingEntry> entry in CASC.EncodingHandler.Entries) {
                    string md5 = entry.Key.ToHexString();
                    
                    writer.WriteLine(md5);
                }
            }
            
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.idxhashes")) {
                foreach (KeyValuePair<MD5Hash, IndexEntry> entry in CASC.LocalIndex.LocalIndexData) {
                    string md5 = entry.Key.ToHexString();
                    
                    writer.WriteLine(md5);
                }
            }
        }

        public static void DumpNonBLTE(string[] args) {
            Directory.CreateDirectory(NonBLTEDir);
            foreach (KeyValuePair<MD5Hash, IndexEntry> indexEntry in CASC.LocalIndex.LocalIndexData) {
                string md5 = indexEntry.Key.ToHexString();
                MD5Hash md5Obj = new MD5Hash();

                try {
                    Stream rawStream = CASC.OpenIndexInfo(indexEntry.Value, md5Obj, false);

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

            foreach (KeyValuePair<MD5Hash,IndexEntry> indexEntry in CASC.LocalIndex.LocalIndexData) {
                string md5 = indexEntry.Key.ToHexString();

                if (!otherHashes.Contains(md5)) {
                    try {
                        Stream rawStream = CASC.OpenIndexInfo(indexEntry.Value, md5Obj, false);
                        
                        Stream stream = new BLTEStream(rawStream, md5Obj);
                        
                        TryConvertFile(stream, ConvertIdxDir, md5);

                        stream.Position = 0;

                        using (Stream file = File.OpenWrite(Path.Combine(RawIdxDir, md5))) {
                            stream.CopyTo(file);
                        }
                        
                        rawStream.Dispose();
                        stream.Dispose();
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

        public static void CompareEnc(string[] args) {
            string otherVerNum = args[2];
            
            HashSet<ulong> missingKeys = new HashSet<ulong>();

            Directory.CreateDirectory(RawEncDir);
            Directory.CreateDirectory(ConvertEncDir);

            string[] otherHashes;
            using (StreamReader reader = new StreamReader($"{otherVerNum}.enchashes")) {
                otherHashes = reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).ToArray();
            }

            foreach (KeyValuePair<MD5Hash, EncodingEntry> entry in CASC.EncodingHandler.Entries) {
                string md5 = entry.Key.ToHexString();

                if (!otherHashes.Contains(md5)) {
                    try {
                        Stream stream = CASC.OpenFile(entry.Value.Key);
                        
                        TryConvertFile(stream, ConvertEncDir, md5);

                        stream.Position = 0;

                        using (Stream file = File.OpenWrite(Path.Combine(RawEncDir, md5))) {
                            stream.CopyTo(file);
                        }
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
                    
                    //Chunked chunked = new Chunked(stream);
                    //if (chunked.Header.StringIdentifier == "MODL".ReverseXor()) {
                    //    OWMDLWriter writer = new OWMDLWriter();
                    //    using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".owmdl")) {
                    //        file.SetLength(0);
                    //        writer.Write(chunked, file, lods, null, new object[] {true, null, null, true, false});
                    //    }
                    //}

                    teChunkedData chunkedData = new teChunkedData(reader);
                    if (chunkedData.Header.StringIdentifier == "MODL") {
                        OverwatchModel model = new OverwatchModel(chunkedData);
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
                    try {
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
                                Console.Out.WriteLine(texture.Header.DataSize);
                            }
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
                foreach (KeyValuePair<ulong, CMFHashData> pair in apm.CMF.Map) {
                    Files[pair.Value.id] = pair.Value.HashKey;
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
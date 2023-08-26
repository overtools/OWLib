using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TankLib;
using TankLib.ExportFormats;
using TankLib.STU;
using TankLib.STU.Types;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Container;
using TACTLib.Core.Key;
using TACTLib.Core.Product.Tank;
using TACTLib.Exceptions;

namespace CASCEncDump {
    internal class Program {
        private static uint BuildVersion;

        private static string BaseDir => Path.Combine(Environment.CurrentDirectory, "dump", BuildVersion.ToString());
        private static string RawIdxDir => Path.Combine(BaseDir, "idx", "raw");
        private static string RawEncDir => Path.Combine(BaseDir, "enc", "raw");
        private static string ConvertIdxDir => Path.Combine(BaseDir, "idx", "convert");
        private static string ConvertEncDir => Path.Combine(BaseDir, "enc", "convert");
        private static string NonBLTEDir => Path.Combine(BaseDir, "nonblte");
        private static string KeyFilesDir => Path.Combine(BaseDir, "keyfiles");
        private static string AllCMFDir => Path.Combine(BaseDir, "allcmf");
        private static string GUIDDir => Path.Combine(BaseDir, "guids");

        private static ClientHandler Client;
        private static ProductHandler_Tank TankHandler;

        public static void Main(string[] args) {
            string overwatchDir = args[0];
            string mode = args[1];
            const string language = "enUS";

            // Usage:
            // {overwatch dir} dump  --  Dump hashes
            // {overwatch dir} compare-enc {other ver num}  --  Extract added files from encoding (requires dump from other version)
            // {overwatch dir} compare-idx {other ver num}  --  Extract added files from indices (requires dump from other version)
            // {overwatch dir} allcmf  --  Extract all files from the cmf

            // casc setup

            TankLib.TACT.LoadHelper.PreLoad();

            ClientCreateArgs createArgs = new ClientCreateArgs {
                SpeechLanguage = language,
                TextLanguage = language,
                Online = false
            };

            if (mode != "allcmf" && mode != "dump-guids" && mode != "compare-guids" && mode != "dump-cmf") {
                createArgs.HandlerArgs = new ClientCreateArgs_Tank {
                    LoadManifest = false
                };
            }

            Client = new ClientHandler(overwatchDir, createArgs);
            TankHandler = (ProductHandler_Tank) Client.ProductHandler;

            TankLib.TACT.LoadHelper.PostLoad(Client);

            BuildVersion = uint.Parse(Client.InstallationInfo.Values["Version"].Split('.').Last());

            switch (mode) {
                case "dump":
                    Dump(args);
                    break;
                case "compare-enc":
                    CompareEnc(args);
                    break;
                case "compare-idx":
                    CompareIdx(args);
                    break;
                case "allcmf":
                    AllCMF(args);
                    break;
                case "dump-guids":
                    DumpGUIDs(args);
                    break;
                case "compare-guids":
                    CompareGUIDs(args);
                    break;
                case "dump-cmf":
                    DumpCMF(args);
                    break;
                default:
                    throw new Exception($"unknown mode: {mode}");
            }
        }

        private static void DumpCMF(string[] args) {
            HashSet<FullKey> cKeys = new HashSet<FullKey>(CASCKeyComparer.Instance);
            foreach (ContentManifestFile contentManifestFile in new[] { TankHandler.m_rootContentManifest, TankHandler.m_textContentManifest, TankHandler.m_speechContentManifest }) {
                if (contentManifestFile == null) continue;
                foreach (ContentManifestFile.HashData hashData in contentManifestFile.m_hashList) {
                    cKeys.Add(hashData.ContentKey);
                }
            }

            Diff.WriteBinaryCKeys($"{BuildVersion}.cmfhashes", cKeys);
            //Diff.WriteBinaryCKeys(TankHandler, $"{BuildVersion}.cmfhashes", guids);
        }

        private static void DumpGUIDs(string[] args) {
            List<ulong> guids = TankHandler.m_assets.Select(x => x.Key).ToList();

            Diff.WriteBinaryGUIDs($"{BuildVersion}.guids", guids);
            //Diff.WriteTextGUIDs(TankHandler, $"{BuildVersion}.guids", guids);
        }

        private static void CompareGUIDs(string[] args) {
            string otherVerNum = args[2];

            Directory.CreateDirectory(GUIDDir); // file name is the version it is compared to

            HashSet<ulong> last;
            using (Stream lastStream = File.OpenRead($"{otherVerNum}.guids")) {
                last = Diff.ReadGUIDs(lastStream);
            }

            List<ulong> added = TankHandler.m_assets.Keys.Except(last).ToList();
            List<ulong> removed = last.Except(TankHandler.m_assets.Keys).ToList();

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

        private static void AllCMF(string[] args) {
            ushort[] types = args.Skip(2).Select(x => ushort.Parse(x, NumberStyles.HexNumber)).ToArray();

            Directory.CreateDirectory(AllCMFDir);
            foreach (KeyValuePair<ulong, ProductHandler_Tank.Asset> asset in TankHandler.m_assets) {
                ushort type = teResourceGUID.Type(asset.Key);
                if (!types.Contains(type)) continue;
                try {
                    using (Stream stream = TankHandler.OpenFile(asset.Key)) {
                        if (stream == null) continue;
                        string typeDir = Path.Combine(AllCMFDir, type.ToString("X3"));
                        Directory.CreateDirectory(typeDir);
                        using (Stream file = File.OpenWrite(Path.Combine(typeDir, teResourceGUID.AsString(asset.Key)))) {
                            stream.CopyTo(file);
                        }
                    }
                } catch (Exception e) {
                    Console.Out.WriteLine(e);
                }
            }
        }

        private static void Dump(string[] args) {
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.enchashes")) {
                foreach (var ckey in Client.EncodingHandler.GetCKeys()) {
                    string md5 = ckey.ToHexString();

                    writer.WriteLine(md5);
                }
            }

            var dynamicContainer = (ContainerHandler)Client.ContainerHandler;
            using (StreamWriter writer = new StreamWriter($"{BuildVersion}.idxhashes")) {
                foreach (KeyValuePair<TruncatedKey, ContainerHandler.IndexEntry> entry in dynamicContainer.GetIndexEntries()) {
                    string md5 = entry.Key.ToHexString();

                    writer.WriteLine(md5);
                }
            }
        }

        private static void CompareIdx(string[] args) {
            string otherVerNum = args[2];

            HashSet<ulong> missingKeys = new HashSet<ulong>();

            Directory.CreateDirectory(RawIdxDir);
            Directory.CreateDirectory(ConvertIdxDir);

            HashSet<FullKey> otherHashes;
            using (Stream stream = File.OpenRead($"{otherVerNum}.idxhashes")) {
                otherHashes = Diff.ReadCKeys(stream);
            }

            HashSet<TruncatedKey> eKeys = new HashSet<TruncatedKey>();
            foreach (FullKey cKey in otherHashes) {
                eKeys.Add(cKey.AsTruncated());
            }

            var dynamicContainer = (ContainerHandler)Client.ContainerHandler;
            foreach (KeyValuePair<TruncatedKey, ContainerHandler.IndexEntry> indexEntry in dynamicContainer.GetIndexEntries()) {
                string md5 = indexEntry.Key.ToHexString();

                if (!eKeys.Contains(indexEntry.Key)) {
                    throw new NotImplementedException();
                    /*try {
                        Stream stream = dynamicContainer.OpenEKey(indexEntry.Key);
                        TryConvertFile(stream, ConvertIdxDir, md5);

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
                    }*/
                }
            }

            Console.Write("done");
            Console.ReadLine();
        }

        private static void CompareEnc(string[] args) {
            string otherVerNum = args[2];

            HashSet<ulong> missingKeys = new HashSet<ulong>();

            Directory.CreateDirectory(RawEncDir);
            Directory.CreateDirectory(ConvertEncDir);

            string[] otherHashes;
            using (StreamReader reader = new StreamReader($"{otherVerNum}.enchashes")) {
                otherHashes = reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }

            HashSet<FullKey> hashSet = new HashSet<FullKey>(CASCKeyComparer.Instance);
            foreach (FullKey hash in otherHashes.Select(FullKey.FromString)) {
                hashSet.Add(hash);
            }

            foreach (var ckey in Client.EncodingHandler.GetCKeys()) {
                if (hashSet.Contains(ckey)) continue;
                try {
                    Stream stream = Client.OpenCKey(ckey);
                    if (stream == null) continue;
                    string md5 = ckey.ToHexString();
                    using (Stream fileStream = File.OpenWrite(Path.Combine(RawEncDir, md5))) {
                        stream.CopyTo(fileStream);
                    }
                    //TryConvertFile(stream, ConvertEncDir, md5);
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

        private static void TryConvertFile(Stream stream, string convertDir, string md5) {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                uint magic = reader.ReadUInt32();

                stream.Position = 0;
                if (magic == teChunkedData.Magic) {
                    teChunkedData chunkedData = new teChunkedData(reader);
                    if (chunkedData.Header.StringIdentifier == "MODL") {
                        OverwatchModel model = new OverwatchModel(chunkedData, 0, "0");
                        using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".owmdl")) {
                            file.SetLength(0);
                            model.Write(file);
                        }
                    }
                } else if (magic == 0x4D4F5649) { // MOVI
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
                                OverwatchModel model = new OverwatchModel(chunkedData, 0);
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
                        if (!texture.PayloadRequired && texture.Header.DataSize <= stream.Length &&
                            (texture.Header.Flags == teTexture.Flags.Tex1D ||
                             texture.Header.Flags == teTexture.Flags.Tex2D ||
                             texture.Header.Flags == teTexture.Flags.Tex3D ||
                             texture.Header.Flags == teTexture.Flags.Cube ||
                             texture.Header.Flags == teTexture.Flags.Array ||
                             texture.Header.Flags == teTexture.Flags.Unk16 ||
                             texture.Header.Flags == teTexture.Flags.Unk32 ||
                             texture.Header.Flags == teTexture.Flags.Unk128) &&
                            texture.Header.Height < 10000 && texture.Header.Width < 10000 && texture.Header.DataSize > 68) {
                            using (Stream file = File.OpenWrite(Path.Combine(convertDir, md5) + ".dds")) {
                                file.SetLength(0);
                                texture.SaveToDDS(file, false, texture.Header.MipCount);
                            }
                        }
                    } catch (Exception) {
                        // fine
                    }

                    try {
                        stream.Position = 0;
                        teStructuredData structuredData = new teStructuredData(stream, true);

                        if (structuredData.GetInstance<STUResourceKey>() != null) {
                            var key = structuredData.GetInstance<STUResourceKey>();

                            Console.Out.WriteLine("found key");
                            var longKey = ulong.Parse(key.m_keyID, NumberStyles.HexNumber);
                            var longRevKey = BitConverter.ToUInt64(BitConverter.GetBytes(longKey).Reverse().ToArray(), 0);
                            var keyValueString = BitConverter.ToString(key.m_key).Replace("-", string.Empty);
                            var keyNameProper = longRevKey.ToString("X16");
                            Console.Out.WriteLine("Added Encryption Key {0}, Value: {1}", keyNameProper, keyValueString);
                        }
                        // if (structuredData.GetInstance<STUHero>() != null) {
                        //
                        // }
                    } catch (Exception) {
                        // fine
                    }
                }
            }
        }
    }
}
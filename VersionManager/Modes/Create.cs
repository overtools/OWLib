using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using DataTool.Helper;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using VersionManager.Data;
using static DataTool.Program;

namespace VersionManager.Modes {
    public class Create {
        public static string Dir = "output\\versions";
        
        public void Run(CreateFlags flags) {
            VersionManifest manifest = CreateBaseVersionManifest();
            
            IO.CreateDirectoryFromFile(Dir+"\\yes.png");

            using (Stream stream = File.OpenWrite(Path.Combine(Dir, $"{BuildVersion}.verman"))) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    manifest.Serialize(writer);
                }
            }
        }

        public VersionManifest CreateBaseVersionManifest() {
            VersionManifest manifest = new VersionManifest {
                BuildVersion = BuildVersion,
                UsedCMF = CASC.RootHandler.LoadedAPMWithoutErrors,
                AssetData = new List<AssetData>(),
                Assets = new List<Asset>()
            };
            
            HashSet<string> missingKeys = new HashSet<string>();
            Dictionary<string, int> keyFileCount = new Dictionary<string, int>();
            
            HashSet<MD5Hash> doneFiles = new HashSet<MD5Hash>(new MD5HashComparer());
            var md5 = MD5.Create();

            if (manifest.UsedCMF) {
                int i = 0;
                foreach (KeyValuePair<ulong, ApplicationPackageManifest.Types.PackageRecord> file in Files) {
                    ContentManifestFile.HashData cmfRecord = CMFMap[file.Key];
                    Asset asset = new Asset {GUID = file.Key, ContentHash = cmfRecord.HashKey};

                    if (!doneFiles.Contains(asset.ContentHash)) {
                        doneFiles.Add(asset.ContentHash);
                        
                        AssetData assetData = new AssetData {ContentHash = asset.ContentHash, TACTKey = 0, PackageRecord = file.Value};
                        
                        try {
                            using (Stream stream = IO.OpenFileUnsafe(file.Value, out assetData.TACTKey)) {
                                BLTEStream blteStream = (BLTEStream) stream;
                                assetData.TACTKey = blteStream.SalsaKey;
                                //var hash = md5.ComputeHash(stream).ToMD5().ToHexString();
                                //var otherHash = asset.ContentHash.ToHexString();
                                //if (hash != otherHash) {
                                //
                                //}
                                //if (file.Value.Flags.HasFlag(ContentFlags.Bundle)) {
                                //    
                                //}
                            }
                        } catch (LocalIndexMissingException) {
                            continue; // i don't have this file installed
                        } catch (BLTEKeyException e) {
                            string keystring = e.MissingKey.ToString("X");
                            if (missingKeys.Add(keystring)) {
                                Console.Out.WriteLine($"new key: {keystring}");
                                keyFileCount[keystring] = 0;
                            }
                            keyFileCount[keystring]++;
                            assetData.TACTKey = e.MissingKey;
                            assetData.HasUnknownKey = true;
                        } catch (Exception e) {
                            Console.Out.WriteLine(e);
                        }
                        
                        //Console.Out.WriteLine(assetData.TACTKey.ToString("X"));
                        
                        manifest.AssetData.Add(assetData);
                    }
                    
                    manifest.Assets.Add(asset);
                    
                    if (i % 100 == 0) {
                        Console.Out.WriteLine($"{i} / {Files.Count}");
                    } 
                    i++;
                }
            } else {
                return null;
                // get from encoding
                int i = 0;
                foreach (KeyValuePair<MD5Hash,EncodingEntry> entry in CASC.EncodingHandler.Entries) {
                    AssetData asset = new AssetData {ContentHash = entry.Key, TACTKey = 0};

                    try {
                        using (Stream stream = CASC.OpenFile(entry.Value.Key)) {
                            asset.TACTKey = ((BLTEStream) stream).SalsaKey;
                        }
                    } catch (LocalIndexMissingException) {
                        continue; // i don't have this file installed
                    } catch (BLTEKeyException e) {
                        if (missingKeys.Add(e.MissingKey.ToString("X"))) {
                            Console.Out.WriteLine($"new key: {e.MissingKey:X}");
                        }
                        asset.TACTKey = e.MissingKey;
                    } catch (Exception e) {
                        Console.Out.WriteLine(e);
                    }

                    manifest.AssetData.Add(asset);
                    if (i % 100 == 0) {
                        Console.Out.WriteLine($"{i} / {CASC.EncodingHandler.Count}");
                    } 
                    i++;
                }
            }

            return manifest;
        }
    }
}
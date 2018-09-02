using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DataTool.FindLogic;
using DataTool.Flag;
using TankLib;
using TankLib.CASC;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static CMFLib.Extensions;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-newents", Description = "Extract new entities (debug)", TrackTypes = new ushort[] {0x3,0xD,0x8f,0x8e}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugNewEntities : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractNewEntities(toolFlags);
        }

        public void AddAdded(Combo.ComboInfo info, List<string> added, ushort type) {
            foreach (ulong key in TrackedFiles[type]) {
                string name = GetFileName(key);
                if (!added.Contains(name)) continue;
                Combo.Find(info, key);
            }
        }
        
        public void AddNew(Combo.ComboInfo info, List<ulong> all, ushort type) {
            foreach (ulong key in TrackedFiles[type]) {
                if (all.Contains(key)) continue;
                Combo.Find(info, key);
            }
        }

        public void AddNewHash(Combo.ComboInfo info, VersionInfo versionInfo, params ushort[] types) {
            MD5HashComparer comparer = new MD5HashComparer();
            Dictionary<MD5Hash, ushort> addedHashes = new Dictionary<MD5Hash, ushort>(comparer);
            Dictionary<MD5Hash, ulong> hashGUIDs = new Dictionary<MD5Hash, ulong>(comparer);
            //var md5 = MD5.Create();
            
            // key = content hash, value = type
            foreach (KeyValuePair<ulong,ApplicationPackageManifest.Types.PackageRecord> file in Files) {
                ushort fileType = teResourceGUID.Type(file.Key);
                //if (!types.Contains(fileType)) continue;
                if (fileType == 0x9C) continue;  // bundle
                if (fileType == 0x77) continue;  // package indice

                ContentManifestFile.HashData cmfRecord = CMFMap[file.Key];

                if (!versionInfo.ContentHashes.Contains(cmfRecord.HashKey)) {
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0xF) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0x1F) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0x2F) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0x3F) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0x4F) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Locale(file.Key) == 0x5F) continue; // ? 
                    if (fileType == 0x4 && teResourceGUID.Platform(file.Key) == 0x8) continue; // effect images

                    //string currentHash = file.Value.ContentHash.ToHexString();
                    
                    addedHashes[cmfRecord.HashKey] = fileType;
                    hashGUIDs[cmfRecord.HashKey] = file.Key; // todo

                    //using (Stream stream = OpenFile(file.Value)) {
                    //    if (stream == null) continue;
                    //    var hash = md5.ComputeHash(stream).ToMD5();
                    //    if (!comparer.Equals(hash, cmfRecord.HashKey)) {
                    //
                    //    }
                    //}
                }
            }

            foreach (KeyValuePair<MD5Hash,ushort> addedHash in addedHashes) {
                if (types.Contains(addedHash.Value)) {
                    ulong guid = hashGUIDs[addedHash.Key];
                    //string name = teResourceGUID.AsString(guid);
                    Combo.Find(info, guid);
                }
            }
        }

        public class VersionInfo {
            public HashSet<MD5Hash> ContentHashes;
            public HashSet<ulong> GUIDs;
        }

        public static VersionInfo GetVersionInfoFake(string path) {
            VersionInfo info = new VersionInfo {
                GUIDs = new HashSet<ulong>(),
                ContentHashes = new HashSet<MD5Hash>(new MD5HashComparer())
            };
            
            using (StreamReader reader = new StreamReader(path)) {
                info.ContentHashes = new HashSet<MD5Hash>(reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.ToByteArray().ToMD5()), new MD5HashComparer());
            }

            return info;
        }
        
        public static VersionInfo GetGUIDVersionInfo(string path) {
            VersionInfo info = new VersionInfo {
                GUIDs = new HashSet<ulong>()
            };
            
            using (StreamReader reader = new StreamReader(path)) {
                info.GUIDs = new HashSet<ulong>(reader.ReadToEnd().Split('\n').Select(x => x.TrimEnd('\r')).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => ulong.Parse(x, NumberStyles.HexNumber)));
            }

            return info;
        }

        public static VersionInfo GetVersionInfo(string path) {
            throw new NotImplementedException();
        }

        public void ExtractNewEntities(ICLIFlags toolFlags) {
            string basePath;
            
            // sorry if this isn't useful for anyone but me.
            // data.json has a list under the key "added_raw" that contains all of the added files.
            
            //const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.18.1.2.42076\\data.json";
            //const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.20.0.2.43435\\data.json";
            //const string dataPath = "D:\\ow\\OverwatchDataManager\\versions\\1.17.0.3.41713\\data.json";

            //VersionInfo versionInfo = GetVersionInfo(dataPath);
            //VersionInfo versionInfo = GetVersionInfoFake(@"D:\Code\Repos\overtool\OWLib-main\CASCEncDump\bin\Debug\44916.cmfhashes");
            VersionInfo versionInfo = GetVersionInfoFake(@"D:\ow\resources\verdata\49154.cmfhashes");
            
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugNewEntities3";
            
            Combo.ComboInfo info = new Combo.ComboInfo();
            //AddNewHash(info, versionInfo, 0x7C);
            AddNewHash(info, versionInfo, 0x4);
            
            SaveLogic.Combo.Save(flags, Path.Combine(basePath, container), info);
            SaveLogic.Combo.SaveAllSoundFiles(flags, Path.Combine(basePath, container, "Sounds"), info);
            SaveLogic.Combo.SaveAllVoiceSoundFiles(flags, Path.Combine(basePath, container, "VoiceSounds"), info);
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, container, "LooseTex"), info);
            SaveLogic.Combo.SaveAllStrings(flags, Path.Combine(basePath, container, "Strings"), info);
        }
    }
}
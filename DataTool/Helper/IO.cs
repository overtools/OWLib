using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using static DataTool.Program;

namespace DataTool.Helper {
    // ReSharper disable once InconsistentNaming
    public static class IO {
        public static string GetValidFilename(string filename) {
            if (filename == null) return null;
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = $@"[{invalidChars}]+";

            string[] reservedWords = {
                "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
                "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            string sanitisedNamePart = Regex.Replace(filename, invalidReStr, "_");

            return reservedWords.Select(reservedWord => $"^{reservedWord}\\.").Aggregate(sanitisedNamePart,
                (current, reservedWordPattern) => Regex.Replace(current, reservedWordPattern, "_reservedWord_.",
                    RegexOptions.IgnoreCase));
        }
        
        public static Dictionary<uint, Dictionary<uint, string>> GUIDTable = new Dictionary<uint, Dictionary<uint, string>>();

        public static void LoadGUIDTable() {
            if (!File.Exists("GUIDNames.csv")) return;
            int i = 0;
            foreach (string line in File.ReadAllLines("GUIDNames.csv")) {
                if (i == 0) {
                    i++;
                    continue;
                }
                string[] parts = line.Split(',');
                string indexString = parts[0];
                string typeString = parts[1];
                string name = parts[2];

                uint index = uint.Parse(indexString, NumberStyles.HexNumber);
                uint type = uint.Parse(typeString, NumberStyles.HexNumber);

                if (!GUIDTable.ContainsKey(type)) {
                    GUIDTable[type] = new Dictionary<uint, string>();
                }
                GUIDTable[type][index] = name;
                
                i++;
            }
        }

        public static string GetFileName(ulong guid) {
            return teResourceGUID.AsString(guid);
        }

        public static void WriteFile(Stream stream, string filename) {
            if (stream == null) return;
            string path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            using (Stream file = File.OpenWrite(filename)) {
                file.SetLength(0); // ensure no leftover data
                stream.CopyTo(file);
            }
        }

        public static void WriteFile(ulong guid, string path) {
            WriteFile(OpenFile(guid), guid, path);
        }

        public static void WriteFile(Stream stream, ulong guid, string path) {
            if (stream == null || guid == 0) {
                return;
            }

            // string filename = GUIDTable.ContainsKey(guid) ? GUIDTable[guid] : GetFileName(guid);
            string filename = GetFileName(guid);
            
            WriteFile(stream, Path.Combine(path, filename));
            
            if (!Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }

            using (Stream file = File.OpenWrite(Path.Combine(path, filename))) {
                stream.CopyTo(file);
            }
        }
        
        public static Dictionary<MD5Hash, byte[]> BundleCache = new Dictionary<MD5Hash, byte[]>(new MD5HashComparer());
        
        public static Stream OpenFile(ApplicationPackageManifest.Types.PackageRecord record) {
            if (!CASC.EncodingHandler.GetEntry(record.LoadHash, out EncodingEntry enc)) return null;

            try {
                if (record.Flags.HasFlag(ContentFlags.Bundle)) {
                    if (!BundleCache.ContainsKey(record.LoadHash)) {
                        using (Stream bundleStream = CASC.OpenFile(enc.Key)) {
                            byte[] buf = new byte[bundleStream.Length];
                            bundleStream.Read(buf, 0, (int)bundleStream.Length);
                            BundleCache[record.LoadHash] = buf;
                        }
                    }
                    MemoryStream stream = new MemoryStream((int)record.Size);
                    stream.Write(BundleCache[record.LoadHash], (int)record.Offset, (int)record.Size);
                    stream.Position = 0;
                    return stream;
                }
                return CASC.OpenFile(enc.Key);
            } catch (Exception e) {
                if (e is BLTEKeyException exception) {
#if DEBUG
                    Debugger.Log(0, "DataTool", $"[DataTool:CASC]: Missing key: {exception.MissingKey:X16}\r\n");
#endif
                }

                return null;
            }
        }
        
        public static Stream OpenFileUnsafe(ApplicationPackageManifest.Types.PackageRecord record, out ulong salsa) {
            salsa = 0;
            if (!CASC.EncodingHandler.GetEntry(record.LoadHash, out EncodingEntry enc)) return null;
            
            Stream fstream = CASC.OpenFile(enc.Key);
            salsa = ((BLTEStream) fstream).SalsaKey;

            return fstream;
        }
        
        public static Stream OpenFile(ulong guid) {
            try {
                return OpenFile(Files[guid]);
            }
            catch {
                return null;
            }
        }

        public static void CreateDirectoryFromFile(string path) {
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(dir)) {
                return;
            }
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        public static string GetString(ulong guid) {
            if (guid == 0) return null;  // don't even try
            try {
                using (Stream stream = OpenFile(Files[guid])) {
                    return stream == null ? null : new teString(stream);
                }
            }
            catch {
                return null;
            }
        }

        public static void MapCMF() {
            if (Root == null || CASC == null) {
                return;
            }
            const string searchString = "rdev";

            Files = new Dictionary<ulong, ApplicationPackageManifest.Types.PackageRecord>();
            TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            CMFMap = new Dictionary<ulong, ContentManifestFile.HashData>();
            foreach (ApplicationPackageManifest apm in CASC.RootHandler.APMFiles) {
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }

                if (Flags?.Language != null) {
                    if (!apm.Name.ToLowerInvariant().Contains("l" + Flags.Language.ToLowerInvariant())) {
                        continue;
                    }
                }
                
                foreach (KeyValuePair<ulong, ApplicationPackageManifest.Types.PackageRecord> pair in apm.FirstOccurence) {
                    ushort type = teResourceGUID.Type(pair.Key);
                    if (!TrackedFiles.ContainsKey(type)) {
                        TrackedFiles[type] = new HashSet<ulong>();
                    }
                    
                    TrackedFiles[type].Add(pair.Key);
                    Files[pair.Value.GUID] = pair.Value;

                    CMFMap[pair.Value.GUID] = apm.CMF.Map[pair.Value.GUID];
                }
            }
        }
    }
}

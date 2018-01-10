using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CASCLib;
using OWLib;
using static DataTool.Program;

namespace DataTool.Helper {
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
            return $"{guid & 0xFFFFFFFFFFFF:X12}.{GUID.Type(guid):X3}";
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

        public static Stream OpenFile(MD5Hash hash) {
            try {
                return CASC.Encoding.GetEntry(hash, out EncodingEntry enc) ? CASC.OpenFile(enc.Key) : null;
            }
            catch (Exception e) {
                if (e is BLTEKeyException exception) {
                    #if DEBUG
                    Debugger.Log(0, "DataTool", $"[DataTool:CASC]: Missing key: {exception.MissingKey:X16}\r\n");
                    #endif
                }
                return null;
            }
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
                    return stream == null ? null : new OWString(stream);
                }
            }
            catch {
                return null;
            }
        }

        public static void MapCMF(bool mapAll=false) {
            if (Root == null || CASC == null) {
                return;
            }

            foreach (APMFile apm in Root.APMFiles) {
                string searchString = Flags.RCN ? "rcn" : "rdev";
                if (!apm.Name.ToLowerInvariant().Contains(searchString)) {
                    continue;
                }
                if (!apm.Name.ToLowerInvariant().Contains("l" + Flags.Language.ToLowerInvariant())) {
                    continue;
                }
                foreach (KeyValuePair<ulong, CMFHashData> pair in apm.CMFMap) {
                    ushort id = GUID.Type(pair.Key);
                    if (TrackedFiles != null && (TrackedFiles.ContainsKey(id) || mapAll)) {
                        if (!TrackedFiles.ContainsKey(id)) TrackedFiles[id] = new HashSet<ulong>();
                        TrackedFiles[id].Add(pair.Value.id);
                    }

                    Files[pair.Value.id] = pair.Value.HashKey;
                }
            }
        }
    }
}

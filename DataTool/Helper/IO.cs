using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TankLib;
using TACTLib.Exceptions;
using static DataTool.Program;

namespace DataTool.Helper {
    public static class IO {
        public static string GetValidFilename(string filename, bool force = true) {
            if (Flags != null && Flags.NoNames && !force) {
                return null;
            }

            if (filename == null) {
                return null;
            }

            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidReStr = $@"[{invalidChars}]+";

            string[] reservedWords = {
                "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
                "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
                "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
            };

            var newFileName = filename.TrimEnd('.');
            string sanitisedNamePart = Regex.Replace(newFileName, invalidReStr, "_");

            return reservedWords.Select(reservedWord => $"^{reservedWord}\\.").Aggregate(sanitisedNamePart,
                (current, reservedWordPattern) => Regex.Replace(current, reservedWordPattern, "_reservedWord_.",
                    RegexOptions.IgnoreCase));
        }

        public static readonly Dictionary<(ulong, ushort), string> GUIDTable = new Dictionary<(ulong, ushort), string>();
        public static readonly Dictionary<ushort, Dictionary<string, ulong>> LocalizedNames = new Dictionary<ushort, Dictionary<string, ulong>>();
        private static readonly Dictionary<ushort, HashSet<string>> IgnoredLocalizedNames = new Dictionary<ushort, HashSet<string>>();

        public static void LoadGUIDTable(bool onlyCanonical) {
            var guidNamesPath = Path.Combine("Static", "GUIDNames.csv");
            if (!File.Exists(guidNamesPath)) {
                TankLib.Helpers.Logger.Warn("GUIDNames", "GUIDNames.csv not found");
                return;
            }

            var csvLines = File.ReadAllLines(guidNamesPath).Skip(1); // skip header
            foreach (string dirtyLine in csvLines) {
                // remove comments
                var line = dirtyLine.Split(';').FirstOrDefault()?.Trim();

                // skip empty lines
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }

                // :modCheck: csv parser
                // sure hope we never have to deal with quotes containing commas
                string[] parts = line.Split(',').Select(x => x.Trim()).ToArray();
                string indexString = parts[0];
                string typeString = parts[1];
                string name = parts[2];
                string canonicalString = parts[3];

                ulong index = ulong.Parse(indexString, NumberStyles.HexNumber);
                ushort type = ushort.Parse(typeString, NumberStyles.HexNumber);
                bool canonical = byte.Parse(canonicalString) == 1;

                // tbh I don't know what this does
                if (onlyCanonical && !canonical) {
                    continue;
                }

                if (!canonical) {
                    name += $"-{index:X}";
                }

                if (GUIDTable.ContainsKey((index, type))) {
                    TankLib.Helpers.Logger.Warn("GUIDNames", $"Duplicate key detected: {indexString}.{typeString}");
                }

                GUIDTable[(index, type)] = name;
            }
        }

        public static void LoadLocalizedNamesMapping() {
            var locPath = Path.Combine("Static", "LocalizedNamesMapping.csv");
            if (!File.Exists(locPath)) {
                TankLib.Helpers.Logger.Warn("LocalizedNames", "LocalizedNamesMapping.csv not found");
                return;
            }


            var csvLines = File.ReadAllLines(locPath).Skip(1); // skip header
            foreach (string dirtyLine in csvLines) {
                // remove comments
                var line = dirtyLine.Split(';').FirstOrDefault()?.Trim();

                // skip empty lines
                if (string.IsNullOrEmpty(line)) {
                    continue;
                }

                string[] parts = line.Split(',').Select(x => x.Trim()).ToArray();
                string indexString = parts[0];
                string typeString = parts[1];
                string name = parts[2];

                var index = ulong.Parse(indexString, NumberStyles.HexNumber);
                ushort type = ushort.Parse(typeString, NumberStyles.HexNumber);

                if (!LocalizedNames.ContainsKey(type)) {
                    LocalizedNames[type] = new Dictionary<string, ulong>();
                    IgnoredLocalizedNames[type] = new HashSet<string>();
                }

                if (LocalizedNames[type].ContainsKey(name) && LocalizedNames[type][name] != index) {
                    TankLib.Helpers.Logger.Warn("LocalizedNames", $"Duplicate localized name with different values??: {indexString}.{typeString} {name}");
                    LocalizedNames[type].Remove(name);
                    IgnoredLocalizedNames[type].Add(name);
                    continue;
                }

                if (IgnoredLocalizedNames[type].Contains(name) || LocalizedNames[type].ContainsKey(name)) {
                    continue;
                }

                LocalizedNames[type][name] = index;
            }
        }

        public static ulong? TryGetLocalizedName(ushort type, string name) {
            if (!LocalizedNames.ContainsKey(type)) return null;

            if (!LocalizedNames[type].TryGetValue(name, out var match)) {
                return null;
            }

            var guid = new teResourceGUID(match);
            guid.SetType(type);
            return guid;
        }

        public static string GetGUIDName(ulong guid) {
            return GetNullableGUIDName(guid) ?? GetFileName(guid);
        }

        public static string GetNullableGUIDName(ulong guid) {
            var index = teResourceGUID.LongKey(guid);
            var type = teResourceGUID.Type(guid);
            return GUIDTable.TryGetValue((index, type), out var name) ? name : null;
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

            try {
                using (Stream file = File.OpenWrite(filename)) {
                    file.SetLength(0); // ensure no leftover data
                    stream.CopyTo(file);
                }
            }
            catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(string text, string filename) {
            if (text == null) return;
            string path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            var bytes = Encoding.Unicode.GetBytes(text);

            try {
                using (Stream file = File.OpenWrite(filename)) {
                    file.SetLength(0); // ensure no leftover data
                    file.Write(bytes, 0, bytes.Length);
                }
            }
            catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(byte[] bytes, string filename) {
            if (bytes == null) return;
            string path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                using (Stream file = File.OpenWrite(filename)) {
                    file.SetLength(0); // ensure no leftover data
                    file.Write(bytes, 0, bytes.Length);
                }
            }
            catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(Memory<byte> bytes, string filename) {
            string path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                using (Stream file = File.OpenWrite(filename)) {
                    file.SetLength(0); // ensure no leftover data
                    file.Write(bytes.Span);
                }
            }
            catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(ulong guid, string path) {
            if (!TankHandler.m_assets.ContainsKey(guid)) return;
            WriteFile(OpenFile(guid), guid, path);
        }

        public static void WriteFile(ulong guid, string path, string filename) {
            if (!TankHandler.m_assets.ContainsKey(guid)) return;
            WriteFile(OpenFile(guid), Path.Combine(path, filename));
        }

        public static void WriteFile(Stream stream, ulong guid, string path) {
            if (stream == null || guid == 0) {
                return;
            }

            // string filename = GUIDTable.ContainsKey(guid) ? GUIDTable[guid] : GetFileName(guid);
            string filename = GetFileName(guid);

            WriteFile(stream, Path.Combine(path, filename));
        }

        public static HashSet<ulong> MissingKeyLog = new HashSet<ulong>();

        public static Stream OpenFile(ulong guid) {
            try {
                return TankHandler.OpenFile(guid);
            }
            catch (Exception e) {
                if (e is BLTEKeyException keyException) {
                    if (MissingKeyLog.Add(keyException.MissingKey) && Debugger.IsAttached) {
                        TankLib.Helpers.Logger.Warn("BLTE", $"Missing key: {keyException.MissingKey:X16}");
                    }
                }

                TankLib.Helpers.Logger.Debug("Core", $"Unable to load file: {guid:X8}");
                return null;
            }
        }

        public static void CreateDirectoryFromFile(string path) {
            if (path == null) return;
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(dir)) {
                return;
            }

            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        // ???????
        public static void CreateDirectorySafe(string david) {
            if (david == null) return;
            string cylde = Path.GetFullPath(david);
            if (string.IsNullOrWhiteSpace(cylde)) {
                return;
            }

            if (!Directory.Exists(cylde)) {
                Directory.CreateDirectory(cylde);
            }
        }

        public static string GetString(ulong guid) {
            if (guid == 0) return null; // don't even try
            try {
                if (Flags != null && Flags.StringsAsGuids)
                    return teResourceGUID.AsString(guid);

                return GetStringInternal(guid);
            }
            catch {
                return null;
            }
        }

        // ffs blizz, why do the names end in a space sometimes, and sometimes have nbsp???
        public static string GetCleanString(ulong guid) {
            var name = GetString(guid);
            return name?.Replace('\u00A0', ' ').TrimEnd(' ');
        }

        public static string GetStringInternal(ulong guid) {
            if (guid == 0) return null; // don't even try
            try {
                using (Stream stream = OpenFile(guid)) {
                    return stream == null ? null : new teString(stream);
                }
            }
            catch {
                return null;
            }
        }

        public static string GetSubtitleString(ulong key) {
            if (key == 0) return null;

            return GetSubtitle(key)?.m_strings?.FirstOrDefault();
        }

        public static teSubtitleThing GetSubtitle(ulong guid) {
            if (guid == 0) return null; // don't even try
            using (var stream = OpenFile(guid)) {
                if (stream == null) return null;
                using (var reader = new BinaryReader(stream)) {
                    return new teSubtitleThing(reader);
                }
            }
        }

        private static readonly string[] _sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        private const string _sizeFormatTemplate = "{0}{1:0.##} {2}";

        public static string FormatByteSize(long size) {
            if (size == 0) {
                return string.Format(_sizeFormatTemplate, null, 0, _sizeSuffixes[0]);
            }

            var absSize = Math.Abs((double) size);
            var fpPower = Math.Log(absSize, 1000);
            var intPower = (int) fpPower;
            var iUnit = intPower >= _sizeSuffixes.Length
                ? _sizeSuffixes.Length - 1
                : intPower;

            var normSize = absSize / Math.Pow(1000, iUnit);

            return string.Format(_sizeFormatTemplate, size < 0 ? "-" : null, normSize, _sizeSuffixes[iUnit]);
        }
    }
}
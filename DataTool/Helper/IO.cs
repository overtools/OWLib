#nullable enable
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
        private static readonly string[] ReservedWords = {
            "CON", "PRN", "AUX", "CLOCK$", "NUL", "COM0", "COM1", "COM2", "COM3", "COM4",
            "COM5", "COM6", "COM7", "COM8", "COM9", "LPT0", "LPT1", "LPT2", "LPT3", "LPT4",
            "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static readonly Regex InvalidChars = new ($@"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]+", RegexOptions.Compiled);

        public static string? GetValidFilename(string? filename) {
            if (filename == null) {
                return null;
            }

            var newFileName = filename.TrimEnd('.');
            string sanitisedNamePart = InvalidChars.Replace(newFileName, "_");

            if (!OperatingSystem.IsWindows()) {
                return sanitisedNamePart;
            }

            foreach (var reservedWord in ReservedWords) {
                if (sanitisedNamePart.Contains(reservedWord, StringComparison.Ordinal)) {
                    sanitisedNamePart = sanitisedNamePart.Replace(reservedWord, "_reservedWord_.", StringComparison.Ordinal);
                }
            }

            return sanitisedNamePart;
        }

        public static readonly Dictionary<(ulong, ushort), string> GUIDTable = new ();
        public static readonly Dictionary<ushort, Dictionary<string, ulong>> LocalizedNames = new ();
        private static readonly Dictionary<ushort, HashSet<string>> IgnoredLocalizedNames = new ();

        public static void LoadGUIDTable(bool onlyCanonical) {
            var guidNamesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Static", "GUIDNames.csv");
            if (!File.Exists(guidNamesPath)) {
                TankLib.Helpers.Logger.Warn("GUIDNames", "GUIDNames.csv not found");
                return;
            }

            GUIDTable.Clear();
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
            var locPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Static", "LocalizedNamesMapping.csv");
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

        public static teResourceGUID? TryGetLocalizedName(ushort type, string name) {
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

        public static string? GetNullableGUIDName(ulong guid) {
            var index = teResourceGUID.LongKey(guid);
            var type = teResourceGUID.Type(guid);
            return GUIDTable.TryGetValue((index, type), out var name) ? name : null;
        }

        public static string GetFileName(ulong guid) {
            return teResourceGUID.AsString(guid);
        }

        public static Stream? OpenFile(string filename) {
            string? path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                return File.OpenWrite(filename);
            } catch (IOException) {
                if (File.Exists(filename)) return null;
                throw;
            }
        }

        public static void WriteFile(Stream? stream, string filename) {
            if (stream == null) return;
            string? path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                using Stream file = File.OpenWrite(filename);
                file.SetLength(0); // ensure no leftover data
                stream.CopyTo(file);
            } catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(string? text, string filename) {
            if (text == null) return;
            string? path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            var bytes = Encoding.Unicode.GetBytes(text);

            try {
                using Stream file = File.OpenWrite(filename);
                file.SetLength(0); // ensure no leftover data
                file.Write(bytes, 0, bytes.Length);
            } catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(byte[]? bytes, string filename) {
            if (bytes == null) return;
            string? path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                using Stream file = File.OpenWrite(filename);
                file.SetLength(0); // ensure no leftover data
                file.Write(bytes, 0, bytes.Length);
            } catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static void WriteFile(Memory<byte> bytes, string filename) {
            string? path = Path.GetDirectoryName(filename);
            if (!Directory.Exists(path) && path != null) {
                Directory.CreateDirectory(path);
            }

            try {
                using Stream file = File.OpenWrite(filename);
                file.SetLength(0); // ensure no leftover data
                file.Write(bytes.Span);
            } catch (IOException) {
                if (File.Exists(filename)) return;
                throw;
            }
        }

        public static bool AssetExists(ulong guid) {
            return TankHandler.m_assets.ContainsKey(guid);
        }

        public static void WriteFile(ulong guid, string path) {
            if (!AssetExists(guid)) return;
            WriteFile(OpenFile(guid), guid, path);
        }

        public static void WriteFile(ulong guid, string path, string filename) {
            if (!AssetExists(guid)) return;
            WriteFile(OpenFile(guid), Path.Combine(path, filename));
        }

        public static void WriteFile(Stream? stream, ulong guid, string path) {
            if (stream == null || guid == 0) {
                return;
            }

            // string filename = GUIDTable.ContainsKey(guid) ? GUIDTable[guid] : GetFileName(guid);
            string filename = GetFileName(guid);

            WriteFile(stream, Path.Combine(path, filename));
        }

        public static HashSet<ulong> MissingKeyLog = new ();

        public static Stream? OpenFile(ulong guid) {
            if (guid == 0) return null;
            
            try {
                var stream = TankHandler.OpenFile(guid);
                if (stream == null) TankLib.Helpers.Logger.Debug("Core", $"Unable to load file: {guid:X16} - returned null");
                return stream;
            } catch (Exception e) {
                switch (e) {
                    case BLTEKeyException keyException: {
                        if (MissingKeyLog.Add(keyException.MissingKey) && Debugger.IsAttached) {
                            TankLib.Helpers.Logger.Warn("BLTE", $"Missing key: {keyException.MissingKey:X16}");
                        }
                        TankLib.Helpers.Logger.Debug("Core", $"Unable to load file: {guid:X16} - encrypted");
                        return null;
                    }
                    case FileNotFoundException:
                        TankLib.Helpers.Logger.Debug("Core", $"Unable to load file: {guid:X16} - not found");
                        return null;
                    default:
                        TankLib.Helpers.Logger.Debug("Core", $"Unable to load file: {guid:X16} - {e}");
                        return null;
                }
            }
        }

        public static void CreateDirectoryFromFile(string? path) {
            if (path == null) return;
            string? dir = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(dir)) {
                return;
            }

            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        // ???????
        public static void CreateDirectorySafe(string? david) {
            if (david == null) return;
            string cylde = Path.GetFullPath(david);
            if (string.IsNullOrWhiteSpace(cylde)) {
                return;
            }

            if (!Directory.Exists(cylde)) {
                Directory.CreateDirectory(cylde);
            }
        }

        /// <summary>
        /// Returns a string for the guid. NBSPs and trailing nulls are removed.
        /// </summary>
        public static string? GetString(ulong guid) {
            if (guid == 0) return null;
            try {
                if (Flags != null && Flags.StringsAsGuids)
                    return teResourceGUID.AsString(guid);

                // remove nbsp and trailing nulls which can cause issues
                return GetStringInternal(guid)?.Replace('\u00A0', ' ').TrimEnd('\0');
            } catch {
                return null;
            }
        }

        // ffs blizz, why do the names end in a space sometimes, and sometimes have nbsp???
        public static string? GetCleanString(ulong guid) {
            var name = GetString(guid);
            return name?.TrimEnd(' ');
        }

        /// <summary>
        /// Returns the raw string for the guid.
        /// </summary>
        public static string? GetStringInternal(ulong guid) {
            if (guid == 0) return null;
            try {
                using Stream? stream = OpenFile(guid);
                return stream == null ? null : new teString(stream);
            } catch {
                return null;
            }
        }

        public static string? GetSubtitleString(ulong key) {
            if (key == 0) return null;
            return GetSubtitle(key)?.m_strings?.FirstOrDefault();
        }

        public static teSubtitleThing? GetSubtitle(ulong guid) {
            if (guid == 0) return null;
            using var stream = OpenFile(guid);
            if (stream == null) return null;
            using var reader = new BinaryReader(stream);
            return new teSubtitleThing(reader);
        }
    }
}
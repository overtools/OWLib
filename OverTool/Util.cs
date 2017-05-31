using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CASCExplorer;
using OWLib;

namespace OverTool {
    public class Util {
        public static void CopyBytes(Stream i, Stream o, int sz) {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        private static string TypeAlias(ushort type) {
            switch (type) {
                case 0x3: return "Game Logic";
                case 0x4: return "Texture";
                case 0x6: return "Animation";
                case 0x8: return "Material";
                case 0xC: return "Model";
                case 0xD: return "Effect";
                case 0x1A: return "Material Metadata";
                case 0x1B: return "Game Parameter";
                case 0x20:
                case 0x21: return "Animation Metadata";
                case 0x3F:
                case 0x43:
                case 0xB2:
                case 0xBB: return "Audio";
                case 0xBC: return "Map Chunk";
                case 0xA5: return "Cosmetic";
                case 0xA6:
                case 0xAD: return "Texture Override";
                case 0x75: return "Hero Metadata";
                case 0x9F: return "Map Metadata";
                default: return "Unknown";
            }
        }

        public static string DowncaseDiacritics(string txt) {
            var norm = txt.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(norm.Length);
            foreach (char c in norm) {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) {
                    sb.Append(c);
                }
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        public static Stream OpenFile(Record record, CASCHandler handler, bool recur = true) {
            long offset = 0;
            EncodingEntry enc;
            if (((ContentFlags)record.record.Flags & ContentFlags.Bundle) == ContentFlags.Bundle) {
                offset = record.record.Offset;
                handler.Encoding.GetEntry(record.index.bundleContentKey, out enc);
            } else {
                handler.Encoding.GetEntry(record.record.ContentKey, out enc);
            }
            MemoryStream ms = new MemoryStream(record.record.Size);

            try {
                Stream fstream = handler.OpenFile(enc.Key);
                fstream.Position = offset;
                CopyBytes(fstream, ms, record.record.Size);
                ms.Position = 0;
            } catch (Exception ex) {
                if (recur) {
                    OwRootHandler ow = (OwRootHandler)handler.Root;
                    foreach (APMFile apm in ow.APMFiles) {
                        if (!apm.Name.ToLowerInvariant().Contains("rdev")) {
                            continue; // skip
                        }
                        for (int i = 0; i < apm.Packages.Length; ++i) {
                            APMPackage package = apm.Packages[i];
                            PackageIndex index = apm.Indexes[i];
                            PackageIndexRecord[] records = apm.Records[i];
                            for (long j = 0; j < records.LongLength; ++j) {
                                PackageIndexRecord recordindex = records[j];
                                if (recordindex.Key != record.record.Key) {
                                    continue;
                                }

                                Stream strm = OpenFile(new Record {
                                    package = package,
                                    index = index,
                                    record = recordindex,
                                }, handler, false);
                                if (strm != null) {
                                    return strm;
                                }
                            }
                        }
                    }
                    Console.Out.WriteLine("Error {0} with file {2:X12}.{3:X3} ({1})", ex.Message, TypeAlias(GUID.Type(record.record.Key)), GUID.LongKey(record.record.Key), GUID.Type(record.record.Key));
                }
                return null;
            }
            if (System.Diagnostics.Debugger.IsAttached) {
                System.Diagnostics.Debugger.Log(0, "CASC:IO",
                    $"[CASC:IO] Opened file {GUID.LongKey(record.record.Key):X12}.{GUID.Type(record.record.Key):X3}\n");
            }
            return ms;
        }

        public static string SanitizePath(string name) {
            char[] invalids = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalids));
        }

        public static string SanitizeDir(string name) {
            char[] invalids = Path.GetInvalidPathChars();
            return string.Join("_", name.Split(invalids));
        }

        public static string Strip(string name) {
            return name.TrimEnd(new char[2] { '_', ' ' });
        }

        public static string GetString(ulong key, Dictionary<ulong, Record> map, CASCHandler handler) {
            if (!map.ContainsKey(key)) {
                return null;
            }

            Stream str = OpenFile(map[key], handler);
            OWString ows = new OWString(str);
            return ows.Value;
        }
    }
}

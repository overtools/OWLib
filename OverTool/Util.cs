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
                    Console.Out.WriteLine("Error {0} p 0x{1:X16} f 0x{2:X16}", ex.Message, record.package.packageKey, record.record.Key);
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

        public static string GetString(ulong key, Dictionary<ulong, Record> map, CASCHandler handler, params object[] format) {
            if (!map.ContainsKey(key)) {
                return null;
            }

            Stream str = OpenFile(map[key], handler);
            OWString ows = new OWString(str);
            if (format.Length > 0) {
                try {
                    return ows.Format(format);
                } catch { }
            }
            return ows.Value;
        }
    }
}

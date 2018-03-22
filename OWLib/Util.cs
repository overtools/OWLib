using CASCLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using static CASCLib.ApplicationPackageManifest.Types;

namespace OWLib {
    public static class Util {
        public static bool DEBUG = false;

        public static void DumpStruct<T>(T instance, string padding) {
            Type t = typeof(T);
            foreach (FieldInfo info in t.GetFields()) {
                Console.Out.WriteLine("{0}{1}: {2:X8}", padding, info.Name, info.GetValue(instance));
            }
        }


        public static void MapCMF(OwRootHandler ow, CASCHandler handler, Dictionary<ulong, PackageRecord> map, Dictionary<ushort, List<ulong>> track, string language)
        {
            if (ow == null || handler == null)
            {
                return;
            }

            foreach (ApplicationPackageManifest apm in ow.APMFiles)
            {
                if (!apm.Name.ToLowerInvariant().Contains("rdev"))
                {
                    continue;
                }
                if (language != null && !apm.Name.ToLowerInvariant().Contains("l" + language.ToLowerInvariant()))
                {
                    continue;
                }
                foreach (KeyValuePair<ulong, PackageRecord> pair in apm.FirstOccurence)
                {
                    ushort id = GUID.Type(pair.Key);
                    if (track != null && track.ContainsKey(id))
                    {
                        track[id].Add(pair.Value.GUID);
                    }

                    if (map.ContainsKey(pair.Key))
                    {
                        continue;
                    }

                    map[pair.Key] = pair.Value;
                }
            }
        }

        internal static object GetInstanceField(Type type, object instance, string fieldName) {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }

        public static void DumpSTUFields<T>(T instance, string padding="\t") {  // eww?
            Type t = typeof(T);
            foreach (FieldInfo info in t.GetFields()) {
                object got = info.GetValue(instance);
                if (got == null) {
                    Console.Out.WriteLine("{0}{1}: {2:X8}", padding, info.Name, info.GetValue(instance));
                } else {
                    if (got.GetType().Name == "STUGUID" && got.GetType().Namespace == "STULib.Types.Generic") {
                        ulong key = (ulong)GetInstanceField(got.GetType(), got, "Key");
                        Console.Out.WriteLine($"{padding}{info.Name}: {GUID.AsString(key)}");
                    } else {
                        Console.Out.WriteLine("{0}{1}: {2:X8}", padding, info.Name, info.GetValue(instance).ToString());
                    }

                }
            }
        }

        public static void CopyBytes(Stream i, Stream o, int sz)
        {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        private static string TypeAlias(ushort type)
        {
            switch (type)
            {
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
                case 0x90: return "Encryption Key";
                case 0x9F: return "Map Metadata";
                default: return "Unknown";
            }
        }

        public static Stream OpenFile(PackageRecord record, CASCHandler handler)
        {
            long offset = 0;
            EncodingEntry enc;
            if (record.Flags.HasFlag(ContentFlags.Bundle))
            {
                offset = record.Offset;
            }

            if(!handler.Encoding.GetEntry(record.Hash, out enc))
            {
                return null;
            }

            MemoryStream ms = new MemoryStream((int)record.Size);

            try
            {
                Stream fstream = handler.OpenFile(enc.Key);
                fstream.Position = offset;
                CopyBytes(fstream, ms, (int)record.Size);
                ms.Position = 0;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error {0} with file {2} ({1})", ex.Message, TypeAlias(GUID.Type(record.GUID)), GUID.AsString(record.GUID));
                return null;
            }
            if (System.Diagnostics.Debugger.IsAttached)
            {
                System.Diagnostics.Debugger.Log(0, "CASC:IO",
                    $"[CASC:IO] Opened file {GUID.AsString(record.GUID)}\n");
            }
            return ms;
        }

        public static Stream OpenFile(MD5Hash hash, CASCHandler handler)
        {
            try
            {
                Stream fstream = handler.OpenFile(hash);
                return fstream;
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine("Error {0}", ex.Message);
                return null;
            }
        }

        public static string GetEnumName(Type t, object value, string fallback = "{0}") {
            string v = Enum.GetName(t, value);
            if (v == null) {
                v = string.Format(fallback, value.ToString());
            }
            return v;
        }

        public static string GetVersion() {
            Assembly asm = Assembly.GetAssembly(typeof(GUID));
            AssemblyInformationalVersionAttribute attrib = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            AssemblyFileVersionAttribute file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (attrib == null) {
                return file.Version;
            }
            return file.Version + "-git-" + attrib.InformationalVersion;
        }

        public static MemoryStream CopyStream(Stream input, int sz = 0) {
            if (sz == 0) {
                sz = (int)(input.Length - input.Position);
            }
            byte[] buffer = new byte[sz];
            input.Read(buffer, 0, sz);
            MemoryStream output = new MemoryStream(sz);
            output.Write(buffer, 0, sz);
            buffer = null;
            output.Position = 0;
            return output;
        }
    }
}

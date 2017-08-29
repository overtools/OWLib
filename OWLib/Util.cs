using System;
using System.IO;
using System.Reflection;

namespace OWLib {
    public static class Util {
        public static bool DEBUG = false;

        public static void DumpStruct<T>(T instance, string padding) {
            Type t = typeof(T);
            foreach (FieldInfo info in t.GetFields()) {
                Console.Out.WriteLine("{0}{1}: {2:X8}", padding, info.Name, info.GetValue(instance));
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
                        Console.Out.WriteLine($"{padding}{info.Name}: {GUID.LongKey(key):X12}.{GUID.Type(key):X3}");
                    } else {
                        Console.Out.WriteLine("{0}{1}: {2:X8}", padding, info.Name, info.GetValue(instance).ToString());
                    }

                }
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

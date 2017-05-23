using System;
using System.IO;
using System.Reflection;

namespace OWLib {
    public static class Util {
        public static bool DEBUG = false;

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

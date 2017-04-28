using System;
using System.Reflection;

namespace OWLib {
    public static class Util {
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
    }
}

using System.Reflection;

namespace TankLib {
    public class Util {
        public static string GetVersion() {
            Assembly asm = Assembly.GetAssembly(typeof(teResourceGUID));
            return GetVersion(asm);
        }

        public static string GetVersion(Assembly asm) {
            AssemblyInformationalVersionAttribute attrib = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            AssemblyFileVersionAttribute file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (attrib == null) {
                return file.Version;
            }
            return file.Version + "-git-" + attrib.InformationalVersion;
        }   
    }
}
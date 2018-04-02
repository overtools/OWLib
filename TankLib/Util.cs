using System.Reflection;

namespace TankLib {
    public class Util {
        /// <summary>
        /// Get TankLib version info
        /// </summary>
        /// <returns>Version string</returns>
        public static string GetVersion() {
            Assembly asm = Assembly.GetAssembly(typeof(teResourceGUID));
            return GetVersion(asm);
        }

        /// <summary>
        /// Get assembly version info
        /// </summary>
        /// <param name="asm">Assembly to get info about</param>
        /// <returns>Version string</returns>
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
#nullable enable
using System.IO;
using System.Linq;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.Helper {
    public static class STUHelper {
        public static string? GetDescriptionString(ulong key) {
            if (key == 0) return null;
            var description = GetInstance<STU_7AC5B87B>(key);
            return GetString(description?.m_6E7E23A2);
        }

        public static T? GetInstance<T>(ulong key) where T : STUInstance {
            if (key == 0) return null;
            using teStructuredData? structuredData = OpenSTUSafe(key);
            return structuredData?.GetInstance<T>();
        }

        public static T[]? GetInstances<T>(ulong key) where T : STUInstance {
            if (key == 0) return null;
            using teStructuredData? structuredData = OpenSTUSafe(key);
            return structuredData?.GetInstances<T>().ToArray();
        }

        public static teStructuredData? OpenSTUSafe(ulong key) {
            if (key == 0) return null;
        #if RELEASE
            try {
        #endif
            using Stream? stream = OpenFile(key);
            return stream == null ? null : new teStructuredData(stream);
        #if RELEASE
            } catch (System.Exception) {
                return null;
            }
        #endif
        }
    }
}
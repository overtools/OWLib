using System.IO;
using System.Linq;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.Helper {
    public static class STUHelper {
        public static string GetDescriptionString(ulong key) {
            if (key == 0) return null;
            STU_96ABC153 description = GetInstance<STU_96ABC153>(key);
            return GetString(description?.m_94672A2A);
        }
        
        public static string GetSubtitleString(ulong key) {
            if (key == 0) return null;
            STU_A94C5E3B subtitle = GetInstance<STU_A94C5E3B>(key);
            return subtitle?.m_text;
        }

        public static T GetInstance<T>(ulong key) where T : STUInstance {
            if (key == 0) return null;
            using (teStructuredData structuredData = OpenSTUSafe(key)) {
                return structuredData?.GetInstance<T>();
            }
        }
        
        public static T[] GetInstances<T>(ulong key) where T : STUInstance {
            if (key == 0) return null;
            using (teStructuredData structuredData = OpenSTUSafe(key)) {
                return structuredData?.GetInstances<T>().ToArray();
            }            
        }

        public static teStructuredData OpenSTUSafe(ulong key) {
            if (key == 0) return null;
        #if RELEASE
            try {
        #endif
            using (Stream stream = OpenFile(key)) {
                if (stream == null) return null;
                return new teStructuredData(stream);
            }
        #if RELEASE
            } catch (Exception) {
                return null;
            }
        #endif
        }
    }
}

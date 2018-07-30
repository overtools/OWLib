using System;
using System.IO;
using System.Linq;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.IO;

namespace DataTool.Helper {
    public static class STUHelper {
        public static string GetDescriptionString(ulong key) {
            STU_96ABC153 description = GetInstanceNew<STU_96ABC153>(key);
            return GetString(description?.m_94672A2A);
        }
        
        public static string GetSubtitleString(ulong key) {
            STU_A94C5E3B subtitle = GetInstanceNew<STU_A94C5E3B>(key);
            return subtitle?.m_text;
        }

        public static T GetInstanceNew<T>(ulong key) where T : STUInstance {
            teStructuredData structuredData = OpenSTUSafeNew(key);
            return structuredData?.GetInstance<T>();
        }
        
        public static T[] GetInstancesNew<T>(ulong key) where T : STUInstance {
            teStructuredData structuredData = OpenSTUSafeNew(key);
            return structuredData?.GetInstances<T>().ToArray();
        }

        public static teStructuredData OpenSTUSafeNew(ulong key) {
            try {
                using (Stream stream = OpenFile(key)) {
                    return new teStructuredData(stream);
                }
            } catch (Exception) {
                return null;
            }
        }
    }
}

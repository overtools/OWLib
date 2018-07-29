using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using OWLib;
using STULib;
using STULib.Types;
using STULib.Types.Generic;
using TankLib.STU;
using static DataTool.Program;
using static DataTool.Helper.IO;
using Version2 = STULib.Impl.Version2;

namespace DataTool.Helper {
    public static class STUHelper {
        public static string GetDescriptionString(ulong key) {
            STUDescription description = GetInstance<STUDescription>(key);
            return GetString(description?.String);
        }
        
        public static string GetSubtitleString(ulong key) {
            STUSubtitle subtitle = GetInstance<STUSubtitle>(key);
            return subtitle?.Text;
        }
        
        public static ISTU OpenSTUSafe(ulong key) {
            using (Stream stream = OpenFile(key)) {
                return stream == null ? null : ISTU.NewInstance(stream, BuildVersion);
            }
        }
        
        public static T[] GetAllInstances<T>(ulong key) where T : Common.STUInstance  {
            ISTU stu = OpenSTUSafe(key);
            Version2 ver2 = stu as Version2;
            if (ver2 == null) return null;
            return stu.Instances.Concat(ver2.HiddenInstances).OfType<T>().ToArray();
        }

        [Obsolete("GetInstances<T> is deprecated, please use GetInstancesNew<T> instead.")]
        public static T[] GetInstances<T>(ulong key) where T : Common.STUInstance  {
            ISTU stu = OpenSTUSafe(key);
            return stu?.Instances.OfType<T>().ToArray() ?? new T[0];
        }

        [Obsolete("GetInstance<T> is deprecated, please use GetInstanceNew<T> instead.")]
        public static T GetInstance<T>(ulong key) where T : Common.STUInstance  {
            ISTU stu = OpenSTUSafe(key);
            return stu?.Instances.OfType<T>().FirstOrDefault();
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

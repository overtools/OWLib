using System.IO;
using System.Linq;
using STULib;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static STULib.Types.Generic.Common;

namespace DataTool.Helper {
    public static class STUHelper {
        public static string GetDescriptionString(ulong key) {
            STUDescription description = GetInstance<STUDescription>(key);
            return GetString(description?.String);
        }
        
        public static ISTU OpenSTUSafe(ulong key) {
            using (Stream stream = OpenFile(key)) {
                return stream == null ? null : ISTU.NewInstance(stream, BuildVersion);
            }
        }

        public static T[] GetInstances<T>(ulong key) where T : STUInstance  {
            ISTU stu = OpenSTUSafe(key);
            return stu?.Instances.OfType<T>().ToArray() ?? new T[0];
        }

        public static T GetInstance<T>(ulong key) where T : STUInstance  {
            ISTU stu = OpenSTUSafe(key);
            return stu?.Instances.OfType<T>().FirstOrDefault();
        }
    }
}

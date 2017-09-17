using System.Collections.Generic;
using System.IO;
using System.Linq;
using STULib;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.Helper {
    public class STUHelper {
        public static string GetDescriptionString(ulong key) {
            STUDescription description = GetInstance<STUDescription>(key);
            return GetString(description?.String);
        }

        public static T[] GetInstances<T>(ulong key) {
            using (Stream stream = OpenFile(key)) {
                if (stream == null) return default(T[]);
                ISTU stu = ISTU.NewInstance(stream, BuildVersion);
                IEnumerable<T> insts = stu.Instances.OfType<T>();
                IEnumerable<T> enumerable = insts as T[] ?? insts.ToArray();
                return !enumerable.Any() ? default(T[]) : enumerable.ToArray();
            }
        }

        public static T GetInstance<T>(ulong key) {
            T[] insts = GetInstances<T>(key);
            return !insts.Any() ? default(T) : insts.First();
        }
    }
}
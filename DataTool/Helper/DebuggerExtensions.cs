using TankLib;
using TankLib.STU;

namespace DataTool.Helper {
    public static class DebuggerExtensions {
        public static string GetString(this teStructuredDataAssetRef<ulong> stu) {
            if (teResourceGUID.Type(stu) == 0x7C) {
                return IO.GetString(stu);
            }

            return null;
        }
    }
}

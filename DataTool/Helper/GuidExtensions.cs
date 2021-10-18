using TankLib;
using TankLib.STU;

namespace DataTool.Helper {
    public static class GuidExtensions {
        public static string GetTankString(this ulong? guid) {
            if (guid == null || guid == 0) {
                return null;
            }

            return teResourceGUID.Type(guid.Value) == 0x7C ? IO.GetString(guid.Value) : null;
        }

        public static string GetTankString(this teStructuredDataAssetRef<ulong> guid) {
            return GetTankString(guid?.GUID);
        }

        public static string AsString(this ulong guid) {
            return teResourceGUID.AsString(guid);
        }
    }
}

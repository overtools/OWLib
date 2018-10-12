using System.IO;
using TankView.ViewModel;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;

namespace TankView.Helper {
    public static class IOHelper {
        public static Stream OpenFile(CKey ckey) {
            return DataTool.Program.Client.OpenCKey(ckey);
        }

        public static Stream OpenFile(EKey ekey) {
            return DataTool.Program.Client.OpenEKey(ekey);
        }

        public static Stream OpenFile(ApplicationPackageManifest.PackageRecord packageRecord) {
            return DataTool.Program.TankHandler.OpenFile(packageRecord.GUID);
        }

        public static Stream OpenFile(GUIDEntry entry) {
            return entry.GUID != 0 ? DataTool.Program.TankHandler.OpenFile(entry.GUID) : DataTool.Program.Client.OpenCKey(entry.ContentKey);
        }

        public static Stream OpenFile(ulong guid) {
            return DataTool.Program.TankHandler.OpenFile(guid);
        }

        public static bool HasFile(ulong guid) {
            return DataTool.Program.TankHandler.Assets.ContainsKey(guid);
        }
    }
}

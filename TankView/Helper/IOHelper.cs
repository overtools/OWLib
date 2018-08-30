using System.IO;
using TankView.ViewModel;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;

namespace TankView.Helper {
    public static class IOHelper {
        public static Stream OpenFile(CKey ckey) {
            return MainWindow.Client.OpenCKey(ckey);
        }

        public static Stream OpenFile(EKey ekey) {
            return MainWindow.Client.OpenEKey(ekey);
        }

        public static Stream OpenFile(ApplicationPackageManifest.PackageRecord packageRecord) {
            return MainWindow.TankHandler.OpenFile(packageRecord.GUID);
        }

        public static Stream OpenFile(GUIDEntry entry) {
            return entry.GUID != 0 ? MainWindow.TankHandler.OpenFile(entry.GUID) : MainWindow.Client.OpenCKey(entry.ContentKey);
        }

        public static Stream OpenFile(ulong guid) {
            return MainWindow.TankHandler.OpenFile(guid);
        }

        public static bool HasFile(ulong guid) {
            return MainWindow.TankHandler.Assets.ContainsKey(guid);
        }
    }
}

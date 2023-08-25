using System.IO;
using TankView.ViewModel;

namespace TankView.Helper {
    public static class IOHelper {
        public static Stream OpenFile(GUIDEntry entry) {
            return entry.GUID != 0 ? OpenFile(entry.GUID) : DataTool.Program.Client.OpenCKey(entry.ContentKey);
        }

        public static Stream OpenFile(ulong guid) {
            return DataTool.Program.TankHandler.OpenFile(guid);
        }

        public static bool HasFile(ulong guid) {
            return DataTool.Program.TankHandler.m_assets.ContainsKey(guid);
        }
    }
}

using TACTLib;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;

namespace TankView.ViewModel {
    public class GUIDEntry {
        public string Filename { get; set; }
        public ulong GUID { get; set; }
        public string FullPath { get; set; }
        public int Size { get; set; }
        public string Locale { get; set; }
        public CKey ContentKey { get; set; }
        public ContentFlags Flags { get; set; }
    }
}

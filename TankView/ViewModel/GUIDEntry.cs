using TACTLib.Container;
using TankLib;

namespace TankView.ViewModel {
    public class GUIDEntry {
        public string Filename { get; set; }
        public ulong GUID { get; set; }
        public string FullPath { get; set; }
        public int Size { get; set; }
        public string Locale { get; set; }
        public CKey ContentKey { get; set; }
        public ContentFlags Flags { get; set; }

        public override string ToString() {
            return teResourceGUID.AsString(GUID);
        }

        public static implicit operator ulong(GUIDEntry guid) {
            return guid.GUID;
        }
        
        public static implicit operator GUIDEntry(teResourceGUID guid) {
            return new GUIDEntry {
                GUID = guid
            };
        }
    }
}

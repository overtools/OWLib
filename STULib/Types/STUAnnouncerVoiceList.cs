using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x9FF759FD)]
    public class STUAnnouncerVoiceList : STUInstance {
        [STUField(0x4ABB8A86)]
        public AnnouncerFXEntry[] Entries;

        public class AnnouncerFXEntry {
            [STUField(0xB6C32079)]
            public STUGUID Master;
        }
    }
}

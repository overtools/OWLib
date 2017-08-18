using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x248E334E)]
    public class STUAnnouncerVoiceList : STUInstance {
        [STUField(0xEAEF2657)]
        public AnnouncerFXEntry[] Entries;

        public class AnnouncerFXEntry {
            [STUField(0x3152B773)]
            public STUGUID Master;
        }
    }
}

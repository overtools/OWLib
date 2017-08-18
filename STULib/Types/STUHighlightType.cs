using static STULib.Types.Generic.Common;

namespace STULib.Types {

    public enum HighlightTypeRarity : uint {
        NORMAL = 0,  // Standard
        SPECIAL = 1061997773  // Sharpshooter, Shutdown, Lifesaver
    }
    [STU(0xC0368123)]
    public class STUHighlightType : STUInstance {
        [STUField(0x0E27C815)]
        public STUGUID Name;

        [STUField(0xA8CF697B)]
        public HighlightTypeRarity Rarity;
    }
}

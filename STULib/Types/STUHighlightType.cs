using static STULib.Types.Generic.Common;

namespace STULib.Types {

    public enum HighlightTypeRarity : uint {
        NORMAL = 0,  // Standard
        SPECIAL = 1061997773  // Sharpshooter, Shutdown, Lifesaver
    }
    [STU(0xC25281C3)]
    public class STUHighlightType : STUInstance {
        [STUField(0xCA7E6EDC)]
        public STUGUID Name;

        [STUField(0x69A20070)]
        public HighlightTypeRarity Rarity;
    }
}

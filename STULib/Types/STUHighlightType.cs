using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0xC25281C3)]
    public class STUHighlightType : STUInstance {
        [STUField(0xCA7E6EDC)]
        public STUGUID Name;

        [STUField(0x69A20070)]
        public float Unknown1;

        [STUField(0x5C712614)]
        public float Unknown2;

        [STUField(0x91590545)]
        public STUGUID[] UnkownGUIDArray;

        public bool IsSpecial => Unknown1 > 0;
    }
}

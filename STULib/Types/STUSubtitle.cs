using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x190D4773)]
    public class STUSubtitle : STUInstance {
        [STUField(0xA5249DE6)]  // crc32b = m_text
        public char[] Chars; 

        public string String { get => new string(Chars);}
    }
}

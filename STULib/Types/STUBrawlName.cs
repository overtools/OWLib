using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0xC25281C3)]
    public class STUBrawlName : STUInstance {
        [STUField(0xC08C4427, "m_name")]  // crc32b = m_name
        public STUGUID Name;
    }
}

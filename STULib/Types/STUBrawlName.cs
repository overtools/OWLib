using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x4683D781)]
    public class STUBrawlName : STUInstance {
        [STUField(0xC08C4427)]  // crc32b = m_name
        public STUGUID Name;
    }
}

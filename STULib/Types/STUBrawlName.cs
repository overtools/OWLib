using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x4B259FE1)]
    public class STUBrawlName : STUInstance {
        [STUField(0xB48F1D22, "m_name")]
        public STUGUID Name;
    }
}

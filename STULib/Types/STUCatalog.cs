using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x314BB6F9, "STUCatalog")]
    public class STUCatalog : STUInstance {
        [STUField(0x33422F70)]
        public STUGUID[] Catalog;
    }
}

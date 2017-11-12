using static STULib.Types.Generic.Common;

namespace STULib.Types {
    [STU(0x314BB6F9, "STUCatalog")]  // todo: this is using old hashes, idk which file this is from.
    public class STUCatalog : STUInstance {
        [STUField(0x33422F70, ForceNotBuffer = true, Demangle = false)]  // todo check, uses u64 unmangled guid
        public STUGUID[] Catalog;
    }
}

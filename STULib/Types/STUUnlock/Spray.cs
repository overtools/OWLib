using OWLib.Types;
using static STULib.Types.Generic.Common;

namespace STULib.Types.STUUnlock {
    [STU(0x15720E8A)]
    public class Spray : Cosmetic {
        public ulong Unknown1;
        public OWRecord EffectResource;
        public OWRecord DecalResource;
        public ulong Unknown2;
    }
    
    [STU(0x6DA011A1)]
    public class SprayMipmap  : STUInstance {
        public OWRecord EffectResource;
        public OWRecord DecalResource;
    }
}

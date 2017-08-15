using OWLib.Types;
using static STULib.Types.Generic.Common;

namespace STULib.Types.STUUnlock {
    [STU(0xE533D614, "STUUnlock_Emote")]
    public class Emote : Cosmetic {
        public STUGUID UnknownRecord014;
        public STUGUID AnimationResource;
        public STUGUID UnknownRecord;
        public STUGUID[] SubData;
        public long Unknown1;
        public uint Unknown2;
        public float Duration;
        public Vec4 Offset;
    }
}

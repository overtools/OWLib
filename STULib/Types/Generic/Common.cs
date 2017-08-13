namespace STULib.Types.Generic {
    public static class Common {
        public class STUInstance {
            public uint InstanceChecksum;
            public uint NextInstanceOffset;

            public override string ToString() {
                return ISTU.GetName(GetType());
            }
        }
    }
}

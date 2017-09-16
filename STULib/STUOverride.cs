using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public class STUOverride : Attribute {
        public uint Checksum;
        public uint Size;

        public STUOverride(uint checksum, uint size) {
            Checksum = checksum;
            Size = size;
        }
    }
}

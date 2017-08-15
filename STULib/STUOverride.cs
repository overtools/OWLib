using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class STUOverride : Attribute {
        public uint Checksum;
        public uint Size;

        public STUOverride(uint Checksum, uint Size) {
            this.Checksum = Checksum;
            this.Size = Size;
        }
    }
}

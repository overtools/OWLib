using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class STUAttribute : Attribute {
        public uint Checksum; // if 0: take CRC of name
        public string Name ; // if null: take struct name

        public STUAttribute() { }

        public STUAttribute(uint checksum) {
            Checksum = checksum;
        }

        public STUAttribute(uint checksum, string name) {
            Checksum = checksum;
            Name = name;
        }
    }
}

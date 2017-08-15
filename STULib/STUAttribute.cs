using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class STUAttribute : Attribute {
        public uint Checksum = 0; // if 0: take CRC of name
        public string Name = null; // if null: take struct name

        public STUAttribute() { }

        public STUAttribute(uint Checksum) {
            this.Checksum = Checksum;
        }

        public STUAttribute(uint Checksum, string Name) {
            this.Checksum = Checksum;
            this.Name = Name;
        }
    }
}

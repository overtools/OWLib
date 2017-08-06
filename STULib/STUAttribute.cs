using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Class)]
    public class STUAttribute : Attribute {
        public uint Checksum = 0; // if 0: take CRC of name
        public string Name = null; // if null: take struct name
    }
}

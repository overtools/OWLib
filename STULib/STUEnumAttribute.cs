using System;
using System.Data.HashFunction.CRCStandards;
using System.Text;

namespace STULib {
    [AttributeUsage(AttributeTargets.Enum)]
    public class STUEnumAttribute : Attribute {
        public uint Checksum;
        public string Name;
        
        public STUEnumAttribute() {}

        public STUEnumAttribute(string name) {
            Checksum = BitConverter.ToUInt32(new CRC32().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant())), 0);
            Name = name;
        }

        public STUEnumAttribute(uint checksum, string name=null) {
            Checksum = checksum;
            Name = name;
            if (name == null) return;
            uint crcCheck = BitConverter.ToUInt32(
                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant())), 0);
            if (checksum != crcCheck) {
                // whatever
            }
        }

        public override string ToString() {
            return $"{Checksum:X}{(Name != null ? $"/{Name}" : "")}";
        }
    }
}

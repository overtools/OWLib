using System;
using System.Data.HashFunction.CRCStandards;
using System.Text;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class STUFieldAttribute : Attribute {
        public bool ReferenceArray = false;
        public bool ReferenceValue = false;
        public object Verify = null;
        public long Padding = 0;
        public int DummySize = -1;  // used to set the size if the field doesn't actually exist.

        public object Default = null;

        public uint Checksum = 0;
        public string Name = null;

        public uint[] IgnoreVersion = null;
        public uint[] STUVersionOnly = null;
        
        public STUFieldAttribute() {}

        public STUFieldAttribute(string Name) {
            Checksum = BitConverter.ToUInt32(new CRC32().ComputeHash(Encoding.ASCII.GetBytes(Name.ToLowerInvariant())), 0);
            this.Name = Name;
        }

        public STUFieldAttribute(uint Checksum, string Name=null) {
            this.Checksum = Checksum;
            this.Name = Name;
            if (Name != null) {
                uint crcCheck = BitConverter.ToUInt32(
                                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(Name.ToLowerInvariant())), 0);
                if (Checksum != crcCheck) {
                    // Debugger.Log(0, "STU", $"[STU] Invalid name for field {Name}, checksum mismatch ({Checksum}, {crcCheck})\n");
                    // No longer the CRC32 of the name :(
                }
            }
        }
    }
}

using System;
using System.Data.HashFunction.CRCStandards;
using System.Diagnostics;
using System.Text;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class STUFieldAttribute : Attribute {
        public bool ReferenceArray = false;
        public bool ReferenceValue = false;
        public object Verify = null;
        public long Padding = 0;

        public object Default = null;

        public uint Checksum = 0;
        public string Name = null;

        public uint[] IgnoreVersion = null;
        public uint[] STUVersionOnly = null;

        public STUFieldAttribute() {}

        public STUFieldAttribute(uint Checksum, string Name=null) {
            this.Checksum = Checksum;
            if (Name != null) {
                uint crcCheck = BitConverter.ToUInt32(
                                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(Name.ToLowerInvariant())), 0);
                if (Checksum != crcCheck) {
                    Debugger.Log(0, "STU", $"[STU] Invalid name for field {Name}, checksum mismatch ({Checksum}, {crcCheck})\n");

                }
            }
        }
    }
}

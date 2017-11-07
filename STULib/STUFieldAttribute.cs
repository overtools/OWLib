using System;
using System.Data.HashFunction.CRCStandards;
using System.Diagnostics;
using System.Text;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field)]
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUFieldAttribute : Attribute {
        public bool ReferenceArray = false;
        public bool ReferenceValue = false;
        public object Verify = null;
        public long Padding = 0;
        public int DummySize = -1;  // used to set the size if the field doesn't actually exist.
        public bool OnlyBuffer = false;
        public bool FakeBuffer = false;  // fake being in an array buffer
        public bool ForceNotBuffer = false;  // fake being out of an array buffer
        public bool Demangle = true;  // this field should be demangled
        public bool EmbeddedInstance = false;  // embedded stu, this is bad code

        public object Default = null;

        public uint Checksum;
        public string Name;

        public uint[] IgnoreVersion = null;
        public uint[] STUVersionOnly = null;
        
        public STUFieldAttribute() {}

        public STUFieldAttribute(string name) {
            Checksum = BitConverter.ToUInt32(new CRC32().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant())), 0);
            Name = name;
        }

        public STUFieldAttribute(uint checksum, string name=null) {
            Checksum = checksum;
            Name = name;
            if (name == null) return;
            uint crcCheck = BitConverter.ToUInt32(
                new CRC32().ComputeHash(Encoding.ASCII.GetBytes(name.ToLowerInvariant())), 0);
            if (checksum != crcCheck) {
                // Debugger.Log(0, "STU", $"[STU] Invalid name for field {Name}, checksum mismatch ({Checksum}, {crcCheck})\n");
                // No longer the CRC32 of the name :(
            }
        }

        internal string DebuggerDisplay => $"{Checksum:X}{(Name != null ? $"/{Name}" : "")}{(DummySize != -1 ? $" DUMMY (DSize: {DummySize})" : "")}";
    }
}

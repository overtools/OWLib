using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class STUElementAttribute : Attribute {
        public bool ReferenceArray = false;
        public bool ReferenceValue = false;
        public object Verify = null;

        public uint Checksum = 0;
        public string[] DependsOn = { };

        public STUElementAttribute() {}

        public STUElementAttribute(uint Checksum) {
            this.Checksum = Checksum;
        }

        public STUElementAttribute(uint Checksum, params string[] DependsOn) {
            this.Checksum = Checksum;
            this.DependsOn = DependsOn;
        }
    }
}

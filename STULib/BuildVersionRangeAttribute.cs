using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BuildVersionRangeAttribute : Attribute {
        public uint Min = 0;
        public uint Max = uint.MaxValue;

        public BuildVersionRangeAttribute() : base() {
        }

        public BuildVersionRangeAttribute(uint Min) : base() {
            this.Min = Min;
        }

        public BuildVersionRangeAttribute(uint Min, uint Max) : base() {
            this.Min = Min;
            this.Max = Max;
        }
    }
}

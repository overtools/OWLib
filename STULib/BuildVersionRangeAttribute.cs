using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class BuildVersionRangeAttribute : Attribute {
        public uint Min;
        public uint Max = uint.MaxValue;

        public BuildVersionRangeAttribute() {
        }

        public BuildVersionRangeAttribute(uint min) {
            Min = min;
        }

        public BuildVersionRangeAttribute(uint min, uint max) {
            Min = min;
            Max = max;
        }
    }
}

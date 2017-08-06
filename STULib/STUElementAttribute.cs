using System;

namespace STULib {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class STUElementAttribute : Attribute {
        public bool ReferenceArray = false;
        public bool ReferenceValue = false;
        public int FlatArray = 0; 
    }
}

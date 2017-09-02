using System;

namespace DataTool.Flag {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class AliasAttribute : Attribute {
        public string Alias;

        public new string ToString() {
            return Alias;
        }
    }
}
